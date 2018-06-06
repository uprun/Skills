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
        private NodeModel CreateNode(SkillsContext context) 
        {
            NodeModel result = null;
            
                var mainNode = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            }
                        );
                context.SaveChanges();
                result = mainNode.Entity;
            
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


        
        private NodeModel AddTagToNode(SkillsContext context, long nodeId, string tag, string value)
        {
            //it definitely should create a new version of node
            var copiedNode = CopyNode(context, nodeId);

            var mainNode = context
                .Nodes
                .Include(n => n.tags)
                .FirstOrDefault(x => x.id == copiedNode.id);

            mainNode
                .tags
                .Add(new TagModel
                    {
                        tag = tag,
                        value = value
                    });
            context.SaveChanges();

            return mainNode;
        }

        private NodeModel CopyNode(SkillsContext context, long nodeId)
        {
            var nodeSource = context
                .Nodes
                .Include(n => n.tags)
                .FirstOrDefault(x => x.id == nodeId);
            
            if(nodeSource == null)
            {
                throw new IndexOutOfRangeException($"Provided node id \"{nodeId}\" is not available.");
            }

            var mainNode = context
                .Nodes
                .Add(
                    new NodeModel 
                    {
                        tags = nodeSource.tags.Select(x => 
                            new TagModel
                            {
                                tag = x.tag,
                                value = x.value 
                            }
                        )
                        .ToList()
                    }
                );
            context.SaveChanges();
            return mainNode.Entity;
        }
        
        [HttpPost]
        public JsonResult AddTag(NodeModel model, string tag, string value)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                result = AddTagToNode(context, model.id, tag, value);
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult Copy(NodeModel model)
        {
            NodeModel result = null;
            using(var context = new SkillsContext())
            {
                result = CopyNode(context, model.id);
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
                        AddTagToNode(context, node.id, "field:list", "toProcess");
                        
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
                        AddTagToNode(context, urlNode.id, "type", "instanceTemplate");
                        AddTagToNode(context, urlNode.id, "%type", "skill");
                        AddTagToNode(context, urlNode.id, "templateVersion", "1.1.0");
                        AddTagToNode(context, urlNode.id, "field:list", "toProcess");
                        AddTagToNode(context, urlNode.id, "name", "");
                        AddTagToNode(context, urlNode.id, "rid:previousVersion", "none");
                    }

                    {
                        var urlNode = CreateNode(context);
                        AddTagToNode(context, urlNode.id, "type", "instanceTemplate");
                        AddTagToNode(context, urlNode.id, "%type", "url");
                        AddTagToNode(context, urlNode.id, "templateVersion", "1.1.0");
                        AddTagToNode(context, urlNode.id, "url", "");
                        AddTagToNode(context, urlNode.id, "rid:previousVersion", "none");
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
                    createdNode = CreateNode(context);
                    foreach(var tag in templateNode.tags)
                    {
                        if(tag.tag.StartsWith("template:"))
                        {
                            string templateTag = tag.tag.Substring("template:".Length);
                            createdNode = AddTagToNode(context, createdNode.id, templateTag, tag.value);
                        }
                    }
                    createdNode = AddTagToNode(context, createdNode.id, "reference:type", templateNode.id.ToString());
                }
                return Json(createdNode);
            }
        }
    }
}
