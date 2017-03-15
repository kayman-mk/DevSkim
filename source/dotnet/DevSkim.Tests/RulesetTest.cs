﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Security.DevSkim;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Collections.Generic;

namespace DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RulesetTest
    {
        [TestMethod]
        public void AddRuleRangeTest()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);            

            // Add Range
            Ruleset testRules = new Ruleset();
            testRules.AddRange(rules.ByLanguage("javascript"));
            Assert.IsTrue(testRules.Count() > 0, "AddRange testRules is empty");

            // Add Rule
            testRules = new Ruleset();
            IEnumerable<Rule> list = rules.ByLanguage("javascript");
            foreach(Rule r in list)
            {
                testRules.AddRule(r);
            }
            
            Assert.IsTrue(testRules.Count() > 0, "AddRule testRules is empty");
        }

        [TestMethod]
        public void AddRuleFromStringAndFile()
        {
            StreamReader fs = File.OpenText(@"rules\custom\todo.json");
            string rule = fs.ReadToEnd();

            // From String
            Ruleset testRules = Ruleset.FromString(rule, "todo.json", null);
            Assert.AreEqual(1, testRules.Count(), "FromString Count should be 1");

            // From File
            testRules = Ruleset.FromFile(@"rules\custom\todo.json", null);
            Assert.AreEqual(1, testRules.Count(), "FromFile Count should be 1");

            foreach (Rule r in testRules)
            {
                Assert.IsNotNull(r.Id);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void InvalidRuleFileFailTest()
        {            
            Ruleset ruleset = Ruleset.FromFile("x:\\file.txt", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidRuleFileFailTest2()
        {
            Ruleset ruleset = Ruleset.FromFile(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void InvalidRuleDirectoryFailTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory("x:\\invalid_directory", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidRuleDirectoryArgsFailTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory(null, null);
        }
    }
}
