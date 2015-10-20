using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iPro.SDK.Client
{
    public class SharedTraceListener : TextWriterTraceListener
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Diagnostics.ConsoleTraceListener" /> class with trace output written to the standard output stream.</summary>
        public SharedTraceListener()
            : base(CustomStringWriter.Instance)
        {
        }
        /// <summary>Initializes a new instance of the <see cref="T:System.Diagnostics.ConsoleTraceListener" /> class with an option to write trace output to the standard output stream or the standard error stream.</summary>
        /// <param name="useErrorStream">true to write tracing and debugging output to the standard error stream; false to write tracing and debugging output to the standard output stream.</param>
        public SharedTraceListener(bool useErrorStream)
            : base(useErrorStream ? Console.Error : CustomStringWriter.Instance)
        {
        }
        /// <summary>Closes the output to the stream specified for this trace listener.</summary>
        public override void Close()
        {
          
        }

         

    }

    public class CustomStringWriter:StringWriter
    {
       // StringBuilder Logs = new StringBuilder();

        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            this.OnWrite(value);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            base.WriteLine(buffer, index, count);
            this.OnWrited(buffer.ToString());
        }

        public override void Write(string format, params object[] arg)
        {
            base.Write(format, arg);
            this.OnWrite(string.Format(format, arg));

        }

        public override void Write(string value)
        {
            base.Write(value);
            this.OnWrite(value);
        }

        void OnWrite(string line)
        {
            if (OnWrited != null)
            {
                this.OnWrited(line);
            }
        }

        public Action<string> OnWrited;

        public static CustomStringWriter Instance = new CustomStringWriter();
    }
}
