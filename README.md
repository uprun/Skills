# CodeName: Skills
Tabs or spaces, don't worry there are no such identation thing here.

Kernighan & Ritchie or ANSI bracket style, don't worry too there are no brackets in this language.

How it is possible - easy you just build your code as a syntax tree.

------------------------------------------
How it all began:
------------------------------------------
A dotnet 2.0 C# project for managing skills.

#NOTE: this is pet project, I am not trying to make it very efficient, I just want to play with some ideas.

The idea behind this project is that I want to keep track of progress in learning some topic, 
but time to time I forget what I have learnt and usually do not keep links to articles which I already read.
And I have a lot of topics in which I am interested in, so I would like to see them all grouped in one place.

#1 idea is to have a record of steps that I have done in process of learning some skill and then just to provide these steps to someone who is interested in same skill.

#2 idea is to play with code and come up with some mix of kanban board and notes with tags which I can search through using some query language, like LINQ.

#3 idea is to have some web-based programming environment so I don't need to install any additional software to test my idea, and it should be super easy to start new project.

#4 idea is that I am tired of inconsistent database and code states like you make some change in database and then code breaks because you deleted/changed type of some column and you then need to fix code as soon as possible because your application just does not work --> so I think that better to have immutable data and classes that is why there are nodes in my project which are representation of virtual memory.

#5 idea is that it should be something trully visual programming independent from any existing language but incorporating all great ideas in functional, object oriented, procedural and logical programming. So mix of LunaLang, Eve, c#, haskell, lisp, prolog, f# and Pascal.

#6 idea is that it should be a all kind of knowledge base, it should support research process, programming and have history of changes.

#7 idea is that git version tracking system is great but sometimes I see that it is not very well suited for code it is not aware of syntax underneath of files, like in case when two people are adding two new functions in same file it may result in conflict in file though their changes are independent; like in case when indentation is changed then it will be removal of one line and with replacing of it with new indented line. --> so I hope to come up with some node based programming and node based code change tracking system that are independent from file representation.

#8 if all above will be implemented then one optimization idea is to generate code straight from node representation.

#9 usually I am interested in a lot of thing and because of this tabs in my browser look like fence ( /\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\/\\ ...)  because of this I would like to have a context for each of topic I am interested in.

And all these ideas I want to mix in this project will see what will be at finish of it.

Inspired by graph knowledge AI labs in ONU, and talks of Alan Kay and Bret Victor, Eve-Lang and Luna-Lang.
