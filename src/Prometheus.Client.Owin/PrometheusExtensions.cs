﻿using System;
using System.Threading.Tasks;
using Prometheus.Client.Collectors;
using Owin;

namespace Prometheus.Client.Owin
{
    /// <summary>
    ///     PrometheusExtensions
    /// </summary>
    public static class PrometheusExtensions
    {

        /// <summary>
        ///     Add PrometheusServer request execution pipeline.
        /// </summary>
        public static IAppBuilder UsePrometheusServer(this IAppBuilder app)
        {
            return UsePrometheusServer(app, new PrometheusOptions());
        }

        /// <summary>
        ///     Add PrometheusServer request execution pipeline.
        /// </summary>
        public static IAppBuilder UsePrometheusServer(this IAppBuilder app, PrometheusOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
             if (!options.MapPath.StartsWith("/"))
                throw new ArgumentException($"MapPath '{options.MapPath}' should start with '/'");
            
            RegisterCollectors(options);

            app.Map(options.MapPath, coreapp =>
            {
                coreapp.Run(async context =>
                {
                    var req = context.Request;
                    var response = context.Response;

                    var acceptHeader = req.Headers.Get("Accept");
                    var acceptHeaders = acceptHeader?.Split(',');
                    var contentType = ScrapeHandler.GetContentType(acceptHeaders);

                    response.ContentType = contentType;

                    using (var outputStream = response.Body)
                    {
                        var collected = options.CollectorRegistryInstance.CollectAll();
                        ScrapeHandler.ProcessScrapeRequest(collected, contentType, outputStream);
                    }

                    await Task.FromResult(0).ConfigureAwait(false);
                });
            });

            return app;
        }


        private static void RegisterCollectors(PrometheusOptions options)
        {
            if (options.UseDefaultCollectors)
            {
                var metricFactory = Metrics.DefaultFactory;
                if (options.CollectorRegistryInstance != CollectorRegistry.Instance)
                    metricFactory = new MetricFactory(options.CollectorRegistryInstance);
                
                options.Collectors.AddRange(DefaultCollectors.Get(metricFactory));
            }

            options.CollectorRegistryInstance.RegisterOnDemandCollectors(options.Collectors);
        }
    }
}