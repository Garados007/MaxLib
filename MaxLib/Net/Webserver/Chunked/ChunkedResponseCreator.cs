﻿using MaxLib.Net.Webserver.Lazy;
using System.Linq;

namespace MaxLib.Net.Webserver.Chunked
{
    public class ChunkedResponseCreator : WebService
    {
        public bool OnlyWithLazy { get; private set; }

        public ChunkedResponseCreator(bool onlyWithLazy = false) : base(WebServiceType.PreCreateResponse)
        {
            OnlyWithLazy = onlyWithLazy;
            if (onlyWithLazy) Importance = WebProgressImportance.High;
        }

        public override bool CanWorkWith(WebProgressTask task)
        {
            return !OnlyWithLazy || (task.Document.DataSources.Count > 0 &&
                task.Document.DataSources.Any((s) => s is LazySource ||
                    (s is Remote.MarshalSource ms && ms.IsLazy)
                ));
        }

        public override void ProgressTask(WebProgressTask task)
        {
            var request = task.Document.RequestHeader;
            var response = task.Document.ResponseHeader;
            response.FieldContentType = task.Document.PrimaryMime;
            response.SetActualDate();
            response.HttpProtocol = request.HttpProtocol;
            response.HeaderParameter["Connection"] = "keep-alive";
            response.HeaderParameter["X-UA-Compatible"] = "IE=Edge";
            response.HeaderParameter["Transfer-Encoding"] = "chunked";
            if (task.Document.PrimaryEncoding != null)
                response.HeaderParameter["Content-Type"] += "; charset=" +
                    task.Document.PrimaryEncoding;
            task.Document.Information.Add("block default response creator", true);
        }
    }
}
