﻿using System.Web.Optimization;

namespace FieldBook.App_Start
{
  public class BundleConfig
  {
    public static void RegisterBundles(BundleCollection bundles)
    {
      bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/bootstrap.css",
                "~/Content/site.css"));


      bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
      "~/Scripts/jquery-{version}.js"));

      bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
            "~/Scripts/modernizr-*"));

      bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
          "~/Scripts/bootstrap.js"));

      bundles.Add(new ScriptBundle("~/bundles/jqueryUnobtrusiveAjax").Include(
            "~/Scripts/jquery.unobtrusive-ajax.js"));
    }
  }
}