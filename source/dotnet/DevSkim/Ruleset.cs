﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Storage for rules
    /// </summary>
    public class Ruleset : IEnumerable<Rule>
    {
        public Ruleset()
        {
            _rules = new List<Rule>();
        }

        /// <summary>
        /// Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromDirectory(string path, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddDirectory(path, tag);

            return result;
        }

        /// <summary>
        /// Load rules from a file
        /// </summary>
        /// <param name="filename">Filename with rules</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromFile(string filename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddFile(filename, tag);

            return result;
        }

        /// <summary>
        /// Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring">JSON string</param>
        /// <param name="sourcename">Name of the source (file, stream, etc..)</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromString(string jsonstring, string sourcename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddString(jsonstring, sourcename, tag);

            return result;
        }

        /// <summary>
        /// Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>        
        public void AddDirectory(string path, string tag)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                this.AddFile(filename, tag);
            }
        }

        /// <summary>
        /// Load rules from a file
        /// </summary>
        /// <param name="filename">Filename with rules</param>
        /// <param name="tag">Tag for the rules</param>
        public void AddFile(string filename, string tag)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("filename");

            if (!File.Exists(filename))
                throw new FileNotFoundException();

            using (StreamReader file = File.OpenText(filename))
            {
                AddString(file.ReadToEnd(), filename, tag);
            }
        }

        /// <summary>
        /// Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring">JSON string</param>
        /// <param name="sourcename">Name of the source (file, stream, etc..)</param>
        /// <param name="tag">Tag for the rules</param>
        public void AddString(string jsonstring, string sourcename, string tag)
        {
            List<Rule> ruleList = new List<Rule>();
            ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring);
            foreach (Rule r in ruleList)
            {
                r.Source = sourcename;
                r.Tag = tag;

                foreach (SearchPattern p in r.Patterns)
                {
                    if (p.PatternType == PatternType.RegexWord || p.PatternType == PatternType.String)
                    {
                        p.PatternType = PatternType.Regex;
                        p.Pattern = string.Format(@"\b{0}\b", p.Pattern);
                    }
                }
            }

            _rules.AddRange(ruleList);
        }

        /// <summary>
        /// Add rule into Ruleset
        /// </summary>
        /// <param name="rule"></param>
        public void AddRule(Rule rule)
        {
            _rules.Add(rule);
        }

        /// <summary>
        /// Adds the elements of the collection to the Ruleset
        /// </summary>
        /// <param name="collection">Collection of rules</param>
        public void AddRange(IEnumerable<Rule> collection)
        {
            _rules.AddRange(collection);
        }

        /// <summary>
        /// Filters rules within Ruleset by language
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Filtered rules</returns>
        public IEnumerable<Rule> ByLanguage(string language)
        {
            // Otherwise preprare the rules for the content type and store it in cache.
            List<Rule> filteredRules = new List<Rule>();

            foreach (Rule r in _rules)
            {
                if (r.AppliesTo != null && r.AppliesTo.Contains(language))
                {
                    // Put rules with defined contenty type (AppliesTo) on top
                    filteredRules.Insert(0, r);
                }
                else if (r.AppliesTo == null || r.AppliesTo.Length == 0)
                {
                    foreach (SearchPattern p in r.Patterns)
                    {
                        // If applies to is defined and matching put those rules first
                        if (p.AppliesTo != null && p.AppliesTo.Contains(language))
                        {
                            filteredRules.Insert(0, r);
                            break;
                        }
                        // Generic rules goes to the end of the list
                        if (p.AppliesTo == null)
                        {
                            filteredRules.Add(r);
                            break;
                        }
                    }
                }
            }

            // Now deal with rule overrides. 
            List<string> idsToRemove = new List<string>();
            foreach (Rule rule in filteredRules)
            {
                if (rule.Overrides != null)
                {
                    foreach (string r in rule.Overrides)
                    {
                        // Mark every rule that is overriden
                        if (!idsToRemove.Contains(r))
                            idsToRemove.Add(r);
                    }
                }
            }

            // Remove marked rules
            foreach (string id in idsToRemove)
            {
                filteredRules.Remove(filteredRules.Find(x => x.Id == id));
            }

            return filteredRules;
        }

        #region IEnumerable interface

        /// <summary>
        /// Count of rules in the ruleset
        /// </summary>        
        public int Count()
        {
            return _rules.Count();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<Rule> IEnumerable<Rule>.GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        #endregion

        #region Fields

        private List<Rule> _rules;
        
        #endregion
    }

}
