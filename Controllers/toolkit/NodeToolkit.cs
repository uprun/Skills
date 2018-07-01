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
            if(!prepared.tags.Exists(x => x.tag == "system-reference:previous"))
            {
                prepared.tags.Add(new TagModel
                {
                    tag = "system-reference:previous",
                    value = nodeId.ToString()
                });
            }
            else
            {
                var systemReferenceTag =  prepared.tags.First(x => x.tag == "system-reference:previous");
                systemReferenceTag.value = nodeId.ToString();
            }
            return prepared;
        }

        public NodeModel SaveNode(SkillsContext context, NodeModelUnsaved node)
        {
            //TODO: Add checks whether node has same type as referenced node
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
                        var correspondingTag = temp.tags.First(x => x.tag == t.tag);
                        if(correspondingTag.value != t.value)
                        {
                            areChanges = true;
                            correspondingTag.value = t.value;
                        }
                    }
                });
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
        public NodeModel AddTag(NodeModel model, string tag, string value)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = PrepareCopyOfNode(context, model.id);
                temp = AddTagToNode(temp, tag, value);
                result = SaveNode(context, temp);
            }
            return result;
        }

        public NodeModel CreateNodeFromTemplate(int nodeId)
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
                    .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null &&
                        n.id == nodeId
                        )
                    .OrderByDescending(n => n.id)
                    .FirstOrDefault();
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