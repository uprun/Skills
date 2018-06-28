using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skills.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;

namespace Skills.Controllers
{
    public class HomeController : Controller
    {
        private NodeModelUnsaved CreateNode(SkillsContext context) 
        {
            NodeModelUnsaved result = new NodeModelUnsaved 
                            {
                                tags = new List<TagModel>()
                            };
            return result;
        }

        [HttpPost]
        public JsonResult MakeNewNode()
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = CreateNode(context);
                result = SaveNode(context, temp);
            }
            return Json(result);
        }


        
        private NodeModelUnsaved AddTagToNode(NodeModelUnsaved unsavedNodeModel, string tag, string value)
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

        private NodeModelUnsaved PrepareCopyOfNode(SkillsContext context, long nodeId)
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

        private NodeModel SaveNode(SkillsContext context, NodeModelUnsaved node)
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
        
        [HttpPost]
        public JsonResult AddTag(NodeModel model, string tag, string value)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = PrepareCopyOfNode(context, model.id);
                temp = AddTagToNode(temp, tag, value);
                result = SaveNode(context, temp);
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult ApplyChanges(NodeModel model)
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
            return Json(result);
        }

        [HttpPost]
        public JsonResult ApplyMigrations()
        {
            using(var context = new SkillsContext())
            {
                
                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "migrationToNodeRepresentation_attempt#1") == null)
                {
                    foreach ( var skill in context.Skills)
                    {
                        var mainNode = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            }
                        );
                        context.SaveChanges();
                        mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "name",
                                value = skill.SkillName
                            }
                        );

                        mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "type",
                                value = "skill"
                            }
                        );
                        context.SaveChanges();
                        var toProcess = context.Skills.Where(x => x.Id == skill.Id).Select(x => x.ToProcess).FirstOrDefault();
                        foreach(var link in toProcess)
                        {
                            var linkToAdd = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            });
                            context.SaveChanges();
                            linkToAdd.Entity.tags.Add(new TagModel
                            {
                                tag = "url",
                                value = link.Url

                            });
                            linkToAdd.Entity.tags.Add(new TagModel
                            {
                                tag = "type",
                                value = "url"
                            });
                            mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "rid:toProcess",
                                value = linkToAdd.Entity.id.ToString()
                            });
                            context.SaveChanges();
                        }
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "migrationToNodeRepresentation_attempt#1",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "nodesAddToProcessField_attempt#1") == null)
                {
                    foreach ( var node in context.Nodes.Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null))
                    {
                        var copied = PrepareCopyOfNode(context, node.id);
                        copied = AddTagToNode(copied, "field:list", "toProcess");
                        SaveNode(context, copied);
                        
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "nodesAddToProcessField_attempt#1",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "default_skills_node_types_attempt#1") == null)
                {
                    {
                        var urlNode = CreateNode(context);
                        urlNode = AddTagToNode(urlNode, "%type", "skill");
                        urlNode = AddTagToNode(urlNode, "templateVersion", "1.1.0");
                        urlNode = AddTagToNode(urlNode, "field:list", "toProcess");
                        urlNode = AddTagToNode(urlNode, "type", "instanceTemplate");
                        urlNode = AddTagToNode(urlNode, "name", "");
                        urlNode = AddTagToNode(urlNode, "rid:previousVersion", "none");
                        SaveNode(context, urlNode);
                    }

                    {
                        var urlNode = CreateNode(context);
                        urlNode = AddTagToNode(urlNode, "type", "instanceTemplate");
                        urlNode = AddTagToNode(urlNode, "%type", "url");
                        urlNode = AddTagToNode(urlNode, "templateVersion", "1.1.0");
                        urlNode = AddTagToNode(urlNode, "url", "");
                        urlNode = AddTagToNode(urlNode, "rid:previousVersion", "none");
                        SaveNode(context, urlNode);
                    }
                    
                    

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "default_skills_node_types_attempt#1",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }
                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "change_node_references") == null)
                {
                    
                    
                    

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "change_node_references",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "change_node_references2") == null)
                {
                    var skillNode = context
                        .Nodes
                        .Include(n => n.tags)
                        .FirstOrDefault(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null &&
                            n.tags.FirstOrDefault(t => t.tag == "templateVersion" && t.value == "1.1.0") != null &&
                            n.tags.FirstOrDefault(t => t.tag == "%type" && t.value == "skill") != null
                        );

                    if(skillNode != null)
                    {
                        var allSkills = context
                            .Nodes
                            .Include(n => n.tags)
                            .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null)
                            .ToList();
                        
                        allSkills.ForEach(x => {
                            var copy = PrepareCopyOfNode(context, x.id);
                            copy = AddTagToNode(copy, "system-reference:type", skillNode.id.ToString());
                            SaveNode(context, copy);
                        });
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "change_node_references2",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "change_node_references3") == null)
                {
                    var skillNode = context
                        .Nodes
                        .Include(n => n.tags)
                        .FirstOrDefault(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null &&
                            n.tags.FirstOrDefault(t => t.tag == "templateVersion" && t.value == "1.1.0") != null &&
                            n.tags.FirstOrDefault(t => t.tag == "%type" && t.value == "skill") != null
                        );

                    if(skillNode != null)
                    {
                        var allSkills = context
                            .Nodes
                            .Include(n => n.tags)
                            .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null)
                            .ToList();
                        
                        allSkills.ForEach(x => {
                            var copy = PrepareCopyOfNode(context, x.id);
                            copy = AddTagToNode(copy, "system-reference:type", skillNode.id.ToString());
                            SaveNode(context, copy);
                        });
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "change_node_references3",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }
                
            }
            return Json(null);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetSkillsAvailable()
        {
            NodeModel[] skills = null;

            using(var context = new SkillsContext())
            {
                
                var allNodes = context.Nodes
                    .Include(n => n.tags)
                    .ToList();
                
                var nodesLookup = allNodes.ToDictionary(n => n.id);
                allNodes.ForEach(n => {
                    var previousTag = n.tags.FirstOrDefault(x => x.tag == "system-reference:previous");
                    if(previousTag != null) 
                    {
                        long previousId;
                        bool parsable = long.TryParse(previousTag.value, out previousId);
                        if(parsable && nodesLookup.ContainsKey(previousId))
                        {
                            nodesLookup[previousId] = null;
                        }
                    }
                });

                var skillTypes = allNodes
                    .Where(x => x.tags.Exists(t => t.tag == "type" && t.value == "instanceTemplate"))
                    .Where(x => x.tags.Exists(t => t.tag == "%type" && t.value == "skill"))
                    .ToDictionary(x => x.id);

                skills = nodesLookup.Values.Where(n => {
                    if(n == null)
                    {
                        return false;
                    }
                    else
                    {
                        var typeTag = n.tags.FirstOrDefault(x => x.tag == "system-reference:type");
                        if(typeTag != null) 
                        {
                            long typeId;
                            bool parsable = long.TryParse(typeTag.value, out typeId);
                            if(parsable && skillTypes.ContainsKey(typeId))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }

                    }
                }).ToArray();
                
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult GetTemplatesAvailable()
        {
            NodeModel[] skills = null;

            using(var context = new SkillsContext())
            {
                skills = context.Nodes
                    .Include(n => n.tags)
                    .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null)
                    .ToArray();
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult CreateNodeFromTemplate(int nodeId)
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
                return Json(createdNode);
            }
        }
    }
}
