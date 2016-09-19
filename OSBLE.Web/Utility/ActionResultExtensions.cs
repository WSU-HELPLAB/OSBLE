using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;


public static class ActionResultExtensions
{
    public static string Capture(this ActionResult result, ControllerContext controllerContext)
    {
        if (controllerContext == null)
        {
            return "";
        }

        using (var capture = new ResponseCapture(controllerContext.RequestContext.HttpContext.Response))
        {
            result.ExecuteResult(controllerContext);
            return capture.ToString();
        }
    }
}

public class ResponseCapture : IDisposable
{
    private readonly HttpResponseBase _response;
    private readonly TextWriter _originalWriter;
    private StringWriter _localWriter;

    public ResponseCapture(HttpResponseBase response)
    {
        _response = response;
        _originalWriter = response.Output;
        _localWriter = new StringWriter();
        response.Output = _localWriter;
    }

    public override string ToString()
    {
        _localWriter.Flush();
        return _localWriter.ToString();
    }

    public void Dispose()
    {
        if (_localWriter == null) return;

        _localWriter.Dispose();
        _localWriter = null;
        _response.Output = _originalWriter;
    }
}