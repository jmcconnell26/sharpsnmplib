/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 12:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;

using Lextm.SharpSnmpLib.Mib;

namespace Lextm.SharpSnmpLib.Compiler
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class CompilerCore : IDisposable
    {
        private readonly IList<string> _files = new List<string>();
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        public CompilerCore()
        {
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += BackgroundWorker1DoWork;
            _worker.RunWorkerCompleted += BackgroundWorker1RunWorkerCompleted;
        }

        public bool IsBusy
        {
            get { return _worker.IsBusy; }
        }

        public Parser Parser { get; set; }

        public Assembler Assembler { get; set; }

        public event EventHandler<EventArgs> RunCompilerCompleted;

        public event EventHandler<FileAddedEventArgs> FileAdded;

        public void Compile(IEnumerable<string> files)
        {
            _worker.RunWorkerAsync(files);
        }
        
        private void BackgroundWorker1DoWork(object sender, DoWorkEventArgs e)
        {
            IEnumerable<string> docs = (IEnumerable<string>)e.Argument;
            IEnumerable<MibException> errors;
            CompileInternal(docs, out errors);
            e.Result = errors;
        }

        private void CompileInternal(IEnumerable<string> docs, out IEnumerable<MibException> errors)
        {
            IEnumerable<IModule> modules = Parser.ParseToModules(docs, out errors);
            Assembler.Assemble(modules);
        }

        private void BackgroundWorker1RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TraceSource source = new TraceSource("Compiler");
            if (e.Result != null)
            {
                IEnumerable<MibException> errors = (IEnumerable<MibException>) e.Result;
                foreach (MibException error in errors)
                {
                    source.TraceInformation(error.Message);
                }
            }

            if (e.Error != null)
            {
                source.TraceInformation(e.Error.Message);
            }

            source.Flush();
            source.Close();
            if (RunCompilerCompleted != null)
            {
                RunCompilerCompleted(this, EventArgs.Empty);
            }

            SystemSounds.Beep.Play();
        }

        public void Add(IEnumerable<string> files)
        {
            IList<string> filered = new List<string>();
            foreach (string file in files.Where(file => !_files.Contains(file)))
            {
                _files.Add(file);
                filered.Add(file);
            }

            if (FileAdded != null)
            {
                FileAdded(this, new FileAddedEventArgs(filered));
            }
        }

        public void CompileAll()
        {
            Compile(_files);
        }

        public void Remove(string name)
        {
            _files.Remove(name);
        }
        
        public void Dispose()
        {
            _worker.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
