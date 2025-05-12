using ModelContextProtocol.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics
{
    internal class ActivityHelper
    {
        private readonly ActivitySource _source;

        public ActivityHelper(ActivitySource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Activity? StartServerActivity(string name, object? context, ActivityContext rootContext, Action<Activity>? configure = null)
        {
            var activity = _source.StartActivity(name, ActivityKind.Server, rootContext);

            if (activity != null)
            {
                configure?.Invoke(activity);
            }

            return activity;
        }        
    }
}
