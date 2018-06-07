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
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
        }
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
                result = CreateNode(context);
            }
            return Json(result);
        }


        
        private NodeModelUnsaved AddTagToNode(SkillsContext context, NodeModelUnsaved unsavedNodeModel, string tag, string value)
        {
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
            return prepared;
        }

        private NodeModel AddNode(SkillsContext context, NodeModelUnsaved node)
        {
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
                result = AddTagToNode(context, temp, tag, value);
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult Copy(NodeModel model)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                var temp = PrepareCopyOfNode(context, model.id);
                result = AddNode(context, temp);
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
                        AddTagToNode(context, copied, "field:list", "toProcess");
                        AddNode(context, copied);
                        
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
                        AddTagToNode(context, urlNode, "type", "instanceTemplate");
                        AddTagToNode(context, urlNode, "%type", "skill");
                        AddTagToNode(context, urlNode, "templateVersion", "1.1.0");
                        AddTagToNode(context, urlNode, "field:list", "toProcess");
                        AddTagToNode(context, urlNode, "name", "");
                        AddTagToNode(context, urlNode, "rid:previousVersion", "none");
                        AddNode(context, urlNode);
                    }

                    {
                        var urlNode = CreateNode(context);
                        AddTagToNode(context, urlNode, "type", "instanceTemplate");
                        AddTagToNode(context, urlNode, "%type", "url");
                        AddTagToNode(context, urlNode, "templateVersion", "1.1.0");
                        AddTagToNode(context, urlNode, "url", "");
                        AddTagToNode(context, urlNode, "rid:previousVersion", "none");
                        AddNode(context, urlNode);
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
                
                skills = context.Nodes
                    .Include(n => n.tags)
                    .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null)
                    .ToArray();
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
            //TODO: remake creation from template in such way that node is created at once
            // When node is created from template then it has reference to it and it should have type name -- done
            // there can be self reference in template for recursive types like "reference:leftSubtree=self" which is subs-
            // tituted with reference to this node
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
                            temp = AddTagToNode(context, temp, templateTag, tag.value);
                        }
                    }
                    temp = AddTagToNode(context, temp, "reference:type", templateNode.id.ToString());
                    createdNode = AddNode(context, temp);
                }
                return Json(createdNode);
            }
        }
    }
}
