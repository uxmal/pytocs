#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Pytocs.Core.TypeInference
{
    /// <summary>
    /// Provides a factory for python source ASTs.  Maintains configurable on-disk and
    /// in-memory caches to avoid re-parsing files during analysis.
    /// </summary>
    public class AstCache
    {
        private readonly IDictionary<string, Module> cache;
        private readonly Analyzer analyzer;
        private readonly IFileSystem fs;
        private readonly string cacheDir;
        private readonly ILogger logger;

        public AstCache(Analyzer analyzer, IFileSystem fs, ILogger logger, string cacheDir)
        {
            this.analyzer = analyzer;
            this.fs = fs;
            this.logger = logger;
            this.cacheDir = cacheDir;
            this.cache = new Dictionary<string, Module>();
        }

        /// <summary>
        /// Clears the memory cache.
        /// </summary>
        public void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Removes all serialized ASTs from the on-disk cache.
        /// </summary>
        /// <returns>
        /// true if all cached AST files were removed
        /// </returns>
        public bool ClearDiskCache()
        {
            try
            {
                fs.DeleteDirectory(cacheDir);
                return true;
            }
            catch (Exception x)
            {
                logger.Error(x, "Failed to clear disk cache. ");
                return false;
            }
        }

        public void Close()
        {
            //        clearDiskCache();
        }

        /// <summary>
        /// Returns the syntax tree for <paramref name="path" />. May find and/or create a
        /// cached copy in the mem cache or the disk cache.
        /// 
        /// <param name="path">Absolute path to a source file.</param>
        /// <returns>The AST, or <code>null</code> if the parse failed for any reason</returns>
        /// </summary>
        public Module GetAst(string path)
        {
            // Cache stores null value if the parse failed.
            if (cache.TryGetValue(path, out var module))
            {
                return module;
            }

            // Might be cached on disk but not in memory.
            module = GetSerializedModule(path);
            if (module != null)
            {
                logger.Verbose("Reusing " + path);
                cache[path] = module;
                return module;
            }

            module = null;
            try
            {
                logger.Verbose("parsing " + path);
                var lexer = new Lexer(path, fs.CreateStreamReader(path));
                var filter = new CommentFilter(lexer);
                var parser = new Parser(path, filter, true, logger);
                var moduleStmts = parser.Parse().ToList();
                int posStart = 0;
                int posEnd = 0;
                if (moduleStmts.Count > 0)
                {
                    posStart = moduleStmts[0].Start;
                    posEnd = moduleStmts.Last().End;
                }
                module = new Module(
                    analyzer.ModuleName(path),
                    new SuiteStatement(moduleStmts, path, posStart, posEnd),
                    path, posStart, posEnd);
            }
            finally
            {
                cache[path] = module!;  // may be null
            }
            return module;
        }

        /// <summary>
        /// Each source file's AST is saved in an object file named for the MD5
        /// checksum of the source file.  All that is needed is the MD5, but the
        /// file's base name is included for ease of debugging.
        /// </summary>
        public string GetCachePath(string sourcePath)
        {
            return fs.makePathString(cacheDir, fs.getFileHash(sourcePath));
        }

        // package-private for testing
        internal Module? GetSerializedModule(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                return null;
            }
            var cached = GetCachePath(sourcePath);
            if (!File.Exists(cached))
            {
                return null;
            }
            return Deserialize(sourcePath);
        }

        // package-private for testing
        Module? Deserialize(string sourcePath)
        {
#if NEVER
        string cachePath = getCachePath(sourcePath);
        FileInputStream fis = null;
        ObjectInputStream ois = null;
        try {
            fis = new FileInputStream(cachePath);
            ois = new ObjectInputStream(fis);
            return (Module) ois.readObject();
        } catch (Exception e) {
            return null;
        } finally {
            try {
                if (ois != null) {
                    ois.close();
                } else if (fis != null) {
                    fis.close();
                }
            } catch (Exception e) {

            }
        }
    }
#endif
            return null;
        }
    }
}