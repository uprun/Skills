using System;
using System.Linq;
using Skills.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Skills.Controllers.toolkit
{
    public class NodeToolkit
    {

        public NodeModelUnsaved PrepareCopyOfNode(SkillsContext context, long nodeId)
        {
            var nodeSource = context
                .Nodes
                .Include(n => n.tags)
                .FirstOrDefault(x => x.id == nodeId);
            
            if(nodeSource == null)
            {
                throw new IndexOutOfRangeException($"Provided node id \"{nodeId}\" is not available.");
            }

            var prepared = new NodeModelUnsaved 
                    {
                        CreatedFrom = nodeId,
                        tags = nodeSource.tags.Select(x => 
                        {
                            if(x.tag != "system-reference:previous" )
                            {
                               return  new TagModel
                                {
                                    tag = x.tag,
                                    value = x.value 
                                };
                            }
                            else
                            {
                                return  new TagModel
                                {
                                    tag = x.tag,
                                    value = nodeId.ToString()
                                };
                            }
                        }
                        )
                        .ToList()
                    };
            return prepared;
        }

        public NodeModel SaveNode(SkillsContext context, NodeModelUnsaved node)
        {
            //TODO: Add checks whether node has same type as referenced node
            if(node.CreatedFrom != null)
            {
                if(!node.tags.Exists(x => x.tag == "system-reference:previous"))
                {
                    node.tags.Add(new TagModel
                    {
                        tag = "system-reference:previous",
                        value = node.CreatedFrom.Value.ToString()
                    });
                }
                else
                {
                    var systemReferenceTag =  node.tags.First(x => x.tag == "system-reference:previous");
                    systemReferenceTag.value = node.CreatedFrom.Value.ToString();
                }
            }
            
            var toSave = new NodeModel
            {
                tags = node.tags
            };
            var added = context.Nodes.Add(toSave);
            context.SaveChanges();
            return added.Entity;
        }

        public NodeModelUnsaved AddTagToNode(NodeModelUnsaved unsavedNodeModel, string tag, string value)
        {
            if(tag == null)
            {
                throw new ArgumentNullException($"{nameof(tag)} is null");
            }
            if(unsavedNodeModel.tags.Exists(x => x.tag == tag))
            {
                throw new ArgumentException($"Cannot add tag {tag} because it is already exists.");
            }
            unsavedNodeModel.tags.Add( new TagModel
            {
                tag = tag,
                value = value
            });

            return unsavedNodeModel;
        }

        public NodeModelUnsaved RemoveTagFromNode(NodeModelUnsaved unsavedNodeModel, string tag)
        {
            if(tag == null)
            {
                throw new ArgumentNullException($"{nameof(tag)} is null");
            }
            if(!unsavedNodeModel.tags.Exists(x => x.tag == tag))
            {
                throw new ArgumentException($"Cannot remove tag {tag} because it does not exist.");
            }
            if(tag.StartsWith("system-reference:"))
            {
                throw new ArgumentOutOfRangeException($"Tag {tag} cannot be removed, because it starts with \"system-reference:\".");
            }
            unsavedNodeModel.tags = unsavedNodeModel.tags.Where(t => t.tag != tag).ToList();
            return unsavedNodeModel;
        }

        public NodeModelUnsaved CreateNode(SkillsContext context) 
        {
            NodeModelUnsaved result = new NodeModelUnsaved 
                            {
                                tags = new List<TagModel>()
                            };
            return result;
        }

        public NodeModel ApplyChanges(NodeModel model)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = PrepareCopyOfNode(context, model.id);
                bool areChanges = false;
                model.tags.ForEach(t => {
                    if(!t.tag.StartsWith("system-reference:"))
                    {
                        var correspondingTag = temp.tags.FirstOrDefault(x => x.tag == t.tag);
                        if(correspondingTag != null)
                        {
                            // edit
                            if(correspondingTag.value != t.value)
                            {
                                areChanges = true;
                                correspondingTag.value = t.value;
                            }
                        }
                        else
                        {
                            // creation of new tag
                            temp = AddTagToNode(temp, t.tag, t.value);
                            areChanges = true;
                        }
                    }
                });
                var toRemove = new Queue<TagModel> ();
                temp.tags.ForEach(t => 
                {
                    var correspondingTag = model.tags.FirstOrDefault(x => x.tag == t.tag);
                    if(correspondingTag == null)
                    {
                        toRemove.Enqueue(t);
                        areChanges = true;
                    }

                });
                foreach( var x in toRemove)
                {
                    temp = RemoveTagFromNode(temp, x.tag);
                }
                if(areChanges)
                {
                    result = SaveNode(context, temp);
                }
                else
                {
                    throw new Exception("No changes found.");
                }
            }
            return result;
        }

        public NodeModel MakeNewNode()
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = CreateNode(context);
                result = SaveNode(context, temp);
            }
            return result;

        }

        public NodeModel CreateNodeFromTemplate(long nodeId)
        {
            // remake creation from template in such way that node is created at once -- done
            // When node is created from template then it has reference to it and it should have type name -- done
            // there can be self reference in template for recursive types like "reference:leftSubtree=self" which is subs-
            // tituted with reference to this node -- done
            // if there is "type" then it is real type name
            // if there is "reference:type" then this is instance of the type
            // if "reference:fieldName" reference node which is type then it mean that this field of specific type


            using(var context = new SkillsContext())
            {
                var templateNode = context.Nodes
                    .Include(n => n.tags)
                    .FirstOrDefault(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null &&
                        n.id == nodeId
                        );
                if(templateNode == null)
                {
                    var thisNode = context.Nodes
                        .Include(n => n.tags)
                        .FirstOrDefault(n => n.id == nodeId);
                    if(thisNode != null)
                    {
                        var templateNodeIdString = thisNode.tags.FirstOrDefault(t => t.tag == "system-reference:type")?.value;
                        if(templateNodeIdString != null)
                        {
                            long templateNodeId;
                            var templateNodeIdParseResult = long.TryParse(templateNodeIdString, out templateNodeId);
                            if(templateNodeIdParseResult)
                            {
                                templateNode = context.Nodes
                                    .Include(n => n.tags)
                                    .FirstOrDefault(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null &&
                                        n.id == templateNodeId
                                        );
                            }
                            else
                            {
                                // Failed value for node  reference
                            }
                            

                        }
                        else
                        {
                            // Node does not have type yet
                        }
                    }
                }
                NodeModel createdNode = null;
                if(templateNode != null )
                {
                    var temp = CreateNode(context);
                    foreach(var tag in templateNode.tags)
                    {
                        if(tag.tag.StartsWith("template:"))
                        {
                            string templateTag = tag.tag.Substring("template:".Length);
                            if(templateTag.StartsWith("reference:") && tag.value == "self")
                            {
                                temp = AddTagToNode(temp, templateTag, templateNode.id.ToString());
                            }
                            else
                            {
                                temp = AddTagToNode(temp, templateTag, tag.value);
                            }
                            
                        }
                    }
                    temp = AddTagToNode(temp, "system-reference:type", templateNode.id.ToString());
                    createdNode = SaveNode(context, temp);
                }
                return createdNode;
            }
        }
    }
}