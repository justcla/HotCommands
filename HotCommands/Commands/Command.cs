//------------------------------------------------------------------------------
// <copyright file="Command.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Operations;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    class Command<T> where T : Command<T> , new()
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        public Package package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static T Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        protected IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new T { package = package  };
        }

        public virtual int HandleCommand(IWpfTextView textView)
        {
            return VSConstants.S_OK;
        }
    }
}
