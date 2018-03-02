﻿/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Alm.Authentication.Git
{
    public struct GitInstallation : IEquatable<GitInstallation>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly StringComparer PathComparer = StringComparer.InvariantCultureIgnoreCase;

        internal const string GitExeName = @"git.exe";
        internal const string AllVersionCmdPath = @"cmd";
        internal const string AllVersionGitPath = @"cmd\" + GitExeName;
        internal const string AllVersionShPath = @"bin\sh.exe";
        internal const string AllVersionBinGitPath = @"bin\" + GitExeName;
        internal const string Version1Config32Path = @"etc\gitconfig";
        internal const string Version2Config32Path = @"mingw32\etc\gitconfig";
        internal const string Version2Config64Path = @"mingw64\etc\gitconfig";
        internal const string Version1Doc32Path = @"doc\git\html";
        internal const string Version2Doc32Path = @"mingw32\share\doc\git-doc";
        internal const string Version2Doc64Path = @"mingw64\share\doc\git-doc";
        internal const string Version1Libexec32Path = @"libexec\git-core\";
        internal const string Version2Libexec32Path = @"mingw32\libexec\git-core";
        internal const string Version2Libexec64Path = @"mingw64\libexec\git-core";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonCmdPaths
           = new Dictionary<KnownGitDistribution, string>
           {
                { KnownGitDistribution.GitForWindows32v1, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionCmdPath },
           };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonConfigPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Config32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Config32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Config64Path },
            };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonGitPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, AllVersionGitPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionGitPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionGitPath },
            };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonLibexecPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Libexec32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Libexec32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Libexec64Path },
            };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonShPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, AllVersionShPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionShPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionShPath },
            };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonDocPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Doc32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Doc32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Doc64Path },
            };

        internal GitInstallation(RuntimeContext context, string path, KnownGitDistribution version)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            if (!CommonConfigPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));
            if (!CommonCmdPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));
            if (!CommonGitPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));
            if (!CommonLibexecPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));
            if (!CommonShPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));
            if (!CommonDocPaths.ContainsKey(version))
                throw new ArgumentOutOfRangeException(nameof(version));

            path = path.TrimEnd('\\');

            // Make sure the GitExeName isn't included as a part of the path.
            if (path.EndsWith(AllVersionGitPath, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - AllVersionGitPath.Length);
            }

            // Versions of git installation could have 2 binaries. One in the `bin` directory and the
            // other in the `cmd` directory. Handle both scenarios.

            if (path.EndsWith(AllVersionBinGitPath, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - AllVersionBinGitPath.Length);
            }

            if (path.EndsWith(GitExeName, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - GitExeName.Length);
            }

            // Trim off trailing '\' characters to increase compatibility.
            path = path.TrimEnd('\\');

            _path = path;
            _distribution = version;
            _cmd = null;
            _config = null;
            _doc = null;
            _git = null;
            _libexec = null;
            _context = context;
            _sh = null;
        }

        private string _config;
        private string _cmd;
        private string _doc;
        private string _git;
        private string _libexec;
        private readonly RuntimeContext _context;
        private string _sh;
        private readonly string _path;
        private readonly KnownGitDistribution _distribution;

        /// <summary>
        /// Gets the path to the installation's gitconfig file (aka system config).
        /// </summary>
        public string Config
        {
            get
            {
                if (_config is null)
                {
                    _config = System.IO.Path.Combine(_path, CommonConfigPaths[_distribution]);
                }
                return _config;
            }
        }

        /// <summary>
        /// Gets the path to the installation's cmd/ folder.
        /// </summary>
        public string Cmd
        {
            get
            {
                if (_cmd is null)
                {
                    _cmd = System.IO.Path.Combine(_path, CommonCmdPaths[_distribution]);
                }
                return _cmd;
            }
        }

        /// <summary>
        /// Gets the path to the installation's doc/ folder.
        /// </summary>
        public string Doc
        {
            get
            {
                if (_doc is null)
                {
                    _doc = System.IO.Path.Combine(_path, CommonDocPaths[_distribution]);
                }
                return _doc;
            }
        }

        /// <summary>
        /// Gets the path to the installation's git.exe file.
        /// </summary>
        public string Git
        {
            get
            {
                if (_git is null)
                {
                    _git = System.IO.Path.Combine(_path, CommonGitPaths[_distribution]);
                }
                return _git;
            }
        }

        /// <summary>
        /// Gets the path to the installation's libexec/ folder.
        /// </summary>
        public string Libexec
        {
            get
            {
                if (_libexec is null)
                {
                    _libexec = System.IO.Path.Combine(_path, CommonLibexecPaths[_distribution]);
                }
                return _libexec;
            }
        }

        /// <summary>
        /// Gets the path to the root of the installation.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Gets the path to the installation's sh.exe file.
        /// </summary>
        public string Sh
        {
            get
            {
                if (_sh == null)
                {
                    _sh = System.IO.Path.Combine(_path, CommonShPaths[_distribution]);
                }
                return _sh;
            }
        }

        /// <summary>
        /// Gets the installation's distribution.
        /// </summary>
        public KnownGitDistribution Version
        {
            get { return _distribution; }
        }

        public override bool Equals(object obj)
        {
            if (obj is GitInstallation other)
                return Equals(other);

            return false;
        }

        public bool Equals(GitInstallation other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_path);
        }

        public override string ToString()
        {
            return _path;
        }

        internal static bool IsValid(GitInstallation value)
        {
            var fs = value._context.FileSystem;

            return fs.DirectoryExists(value._path)
                && fs.DirectoryExists(value.Libexec)
                && fs.FileExists(value.Git);
        }

        public static bool operator ==(GitInstallation install1, GitInstallation install2)
        {
            return install1._distribution == install2._distribution
                && PathComparer.Equals(install1._path, install2._path);
        }

        public static bool operator !=(GitInstallation install1, GitInstallation install2)
        {
            return !(install1 == install2);
        }
    }
}
