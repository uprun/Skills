using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Skills.Models;

namespace Skills.Controllers.nodemigration
{
    public class migration
    {
        public void migrate(Skills.Controllers.toolkit.NodeToolkit toolkit)
        {
            using(var context = new SkillsContext())
            {
                
                // if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "migrationToNodeRepresentation_attempt#1") == null)
                // {
                //     foreach ( var skill in context.Skills)
                //     {
                //         var mainNode = context.Nodes.Add(new NodeModel 
                //             {
                //                 tags = new List<TagModel>()
                //             }
                //         );
                //         context.SaveChanges();
                //         mainNode.Entity.tags.Add(new TagModel
                //             {
                //                 tag = "name",
                //                 value = skill.SkillName
                //             }
                //         );

                //         mainNode.Entity.tags.Add(new TagModel
                //             {
                //                 tag = "type",
                //                 value = "skill"
                //             }
                //         );
                //         context.SaveChanges();
                //         var toProcess = context.Skills.Where(x => x.Id == skill.Id).Select(x => x.ToProcess).FirstOrDefault();
                //         foreach(var link in toProcess)
                //         {
                //             var linkToAdd = context.Nodes.Add(new NodeModel 
                //             {
                //                 tags = new List<TagModel>()
                //             });
                //             context.SaveChanges();
                //             linkToAdd.Entity.tags.Add(new TagModel
                //             {
                //                 tag = "url",
                //                 value = link.Url

                //             });
                //             linkToAdd.Entity.tags.Add(new TagModel
                //             {
                //                 tag = "type",
                //                 value = "url"
                //             });
                //             mainNode.Entity.tags.Add(new TagModel
                //             {
                //                 tag = "rid:toProcess",
                //                 value = linkToAdd.Entity.id.ToString()
                //             });
                //             context.SaveChanges();
                //         }
                //     }

                //     context.VersionsApplied.Add( new VersionModel
                //     { 
                //         VersionApplied = "migrationToNodeRepresentation_attempt#1",
                //         TimeApplied = DateTime.Now
                //     });
                //     context.SaveChanges();

                // }

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "nodesAddToProcessField_attempt#1") == null)
                {
                    foreach ( var node in context.Nodes.Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null))
                    {
                        var copied = toolkit.PrepareCopyOfNode(context, node.id);
                        copied = toolkit.AddTagToNode(copied, "field:list", "toProcess");
                        toolkit.SaveNode(context, copied);
                        
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
                        var urlNode = toolkit.CreateNode(context);
                        urlNode = toolkit.AddTagToNode(urlNode, "%type", "skill");
                        urlNode = toolkit.AddTagToNode(urlNode, "templateVersion", "1.1.0");
                        urlNode = toolkit.AddTagToNode(urlNode, "field:list", "toProcess");
                        urlNode = toolkit.AddTagToNode(urlNode, "type", "instanceTemplate");
                        urlNode = toolkit.AddTagToNode(urlNode, "name", "");
                        urlNode = toolkit.AddTagToNode(urlNode, "rid:previousVersion", "none");
                        toolkit.SaveNode(context, urlNode);
                    }

                    {
                        var urlNode = toolkit.CreateNode(context);
                        urlNode = toolkit.AddTagToNode(urlNode, "type", "instanceTemplate");
                        urlNode = toolkit.AddTagToNode(urlNode, "%type", "url");
                        urlNode = toolkit.AddTagToNode(urlNode, "templateVersion", "1.1.0");
                        urlNode = toolkit.AddTagToNode(urlNode, "url", "");
                        urlNode = toolkit.AddTagToNode(urlNode, "rid:previousVersion", "none");
                        toolkit.SaveNode(context, urlNode);
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
                            var copy = toolkit.PrepareCopyOfNode(context, x.id);
                            copy = toolkit.AddTagToNode(copy, "system-reference:type", skillNode.id.ToString());
                            toolkit.SaveNode(context, copy);
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
                            var copy = toolkit.PrepareCopyOfNode(context, x.id);
                            copy = toolkit.AddTagToNode(copy, "system-reference:type", skillNode.id.ToString());
                            toolkit.SaveNode(context, copy);
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
        }
    }
}