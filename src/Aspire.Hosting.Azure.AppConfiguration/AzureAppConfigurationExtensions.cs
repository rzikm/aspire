// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppConfiguration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AppConfiguration resources to the application model.
/// </summary>
public static class AzureAppConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureAppConfiguration(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.AppConfiguration.AppConfigurationStore"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<AzureAppConfigurationResource>, ResourceModuleConstruct, AppConfigurationStore>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var store = new AppConfigurationStore(name)
            {
                SkuName = "standard",
                DisableLocalAuth = true,
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(store);

            construct.Add(new ProvisioningOutput("appConfigEndpoint", typeof(string)) { Value = store.Endpoint });

            construct.Add(store.CreateRoleAssignment(AppConfigurationBuiltInRole.AppConfigurationDataOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

            var resource = (AzureAppConfigurationResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, store);
        };

        var resource = new AzureAppConfigurationResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
