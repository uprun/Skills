﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Skills.Models;
using System;

namespace Skills.Migrations
{
    [DbContext(typeof(SkillsContext))]
    [Migration("20171202110034_VersionsTable")]
    partial class VersionsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.0-rtm-26452");

            modelBuilder.Entity("Skills.Models.LinkModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("SkillModelId");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("SkillModelId");

                    b.ToTable("LinkModel");
                });

            modelBuilder.Entity("Skills.Models.NodeModel", b =>
                {
                    b.Property<long>("id")
                        .ValueGeneratedOnAdd();

                    b.HasKey("id");

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("Skills.Models.SkillModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MinutesSpent");

                    b.Property<string>("SkillName");

                    b.HasKey("Id");

                    b.ToTable("Skills");
                });

            modelBuilder.Entity("Skills.Models.TagModel", b =>
                {
                    b.Property<long>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("NodeModelid");

                    b.Property<string>("tag");

                    b.Property<string>("value");

                    b.HasKey("id");

                    b.HasIndex("NodeModelid");

                    b.ToTable("TagModel");
                });

            modelBuilder.Entity("Skills.Models.VersionModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("TimeApplied");

                    b.Property<string>("VersionApplied");

                    b.HasKey("Id");

                    b.ToTable("VersionsApplied");
                });

            modelBuilder.Entity("Skills.Models.LinkModel", b =>
                {
                    b.HasOne("Skills.Models.SkillModel")
                        .WithMany("ToProcess")
                        .HasForeignKey("SkillModelId");
                });

            modelBuilder.Entity("Skills.Models.TagModel", b =>
                {
                    b.HasOne("Skills.Models.NodeModel")
                        .WithMany("tags")
                        .HasForeignKey("NodeModelid");
                });
#pragma warning restore 612, 618
        }
    }
}
