﻿namespace OpenStackNetTests.Live
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using OpenStack.Collections;
    using OpenStack.Net;
    using OpenStack.Security.Authentication;
    using OpenStack.Services.Identity;
    using OpenStack.Services.Identity.V2;
    using Rackspace.Services.Identity.V2;

    [TestClass]
    public class IdentityV2Tests
    {
        private LiveTestConfiguration _configuration;

        protected Uri BaseAddress
        {
            get
            {
                if (_configuration == null)
                    return null;

                TestCredentials credentials = _configuration.TryGetSelectedCredentials();
                if (credentials == null)
                    return null;

                return credentials.BaseAddress;
            }
        }

        protected TestProxy Proxy
        {
            get
            {
                if (_configuration == null)
                    return null;

                TestCredentials credentials = _configuration.TryGetSelectedCredentials();
                if (credentials == null)
                    return null;

                return credentials.Proxy;
            }
        }

        protected string Vendor
        {
            get
            {
                if (_configuration == null)
                    return null;

                TestCredentials credentials = _configuration.TryGetSelectedCredentials();
                if (credentials != null)
                    return credentials.Vendor;

                return "OpenStack";
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _configuration = LiveTestConfiguration.LoadDefaultConfiguration();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task TestListExtensions()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                using (IIdentityService service = CreateService())
                {
                    ListExtensionsApiCall apiCall = await service.PrepareListExtensionsAsync(cancellationTokenSource.Token);
                    Tuple<HttpResponseMessage, ReadOnlyCollectionPage<Extension>> response = await apiCall.SendAsync(cancellationTokenSource.Token);

                    Assert.IsNotNull(response);
                    Assert.IsNotNull(response.Item2);

                    ReadOnlyCollectionPage<Extension> extensions = response.Item2;
                    Assert.IsNotNull(extensions);
                    Assert.AreNotEqual(0, extensions.Count);
                    Assert.IsFalse(extensions.CanHaveNextPage);

                    foreach (Extension extension in extensions)
                        CheckExtension(extension);
                }
            }
        }

        [TestMethod]
        public async Task TestListExtensionsSimple()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                using (IIdentityService service = CreateService())
                {
                    ReadOnlyCollectionPage<Extension> extensions = await service.ListExtensionsAsync(cancellationTokenSource.Token);
                    Assert.IsNotNull(extensions);
                    Assert.AreNotEqual(0, extensions.Count);
                    Assert.IsFalse(extensions.CanHaveNextPage);

                    foreach (Extension extension in extensions)
                        CheckExtension(extension);
                }
            }
        }

        [TestMethod]
        public async Task TestGetExtension()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                using (IIdentityService service = CreateService())
                {
                    ListExtensionsApiCall apiCall = await service.PrepareListExtensionsAsync(cancellationTokenSource.Token);
                    Tuple<HttpResponseMessage, ReadOnlyCollectionPage<Extension>> response = await apiCall.SendAsync(cancellationTokenSource.Token);

                    Assert.IsNotNull(response);
                    Assert.IsNotNull(response.Item2);

                    ReadOnlyCollectionPage<Extension> extensions = response.Item2;
                    Assert.IsNotNull(extensions);
                    Assert.AreNotEqual(0, extensions.Count);
                    Assert.IsFalse(extensions.CanHaveNextPage);

                    foreach (Extension listedExtension in extensions)
                    {
                        Assert.IsNotNull(listedExtension);
                        Assert.IsNotNull(listedExtension.Alias);

                        GetExtensionApiCall getApiCall = await service.PrepareGetExtensionAsync(listedExtension.Alias, cancellationTokenSource.Token);
                        Tuple<HttpResponseMessage, ExtensionResponse> getResponse = await getApiCall.SendAsync(cancellationTokenSource.Token);

                        Extension extension = getResponse.Item2.Extension;
                        CheckExtension(extension);
                        Assert.AreEqual(listedExtension.Alias, extension.Alias);
                    }
                }
            }
        }

        [TestMethod]
        public async Task TestGetExtensionSimple()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                using (IIdentityService service = CreateService())
                {
                    ReadOnlyCollectionPage<Extension> extensions = await service.ListExtensionsAsync(cancellationTokenSource.Token);

                    Assert.IsNotNull(extensions);
                    Assert.AreNotEqual(0, extensions.Count);
                    Assert.IsFalse(extensions.CanHaveNextPage);

                    foreach (Extension listedExtension in extensions)
                    {
                        Assert.IsNotNull(listedExtension);
                        Assert.IsNotNull(listedExtension.Alias);

                        Extension extension = await service.GetExtensionAsync(listedExtension.Alias, cancellationTokenSource.Token);
                        CheckExtension(extension);
                        Assert.AreEqual(listedExtension.Alias, extension.Alias);
                    }
                }
            }
        }

        [TestMethod]
        public async Task TestAuthenticate()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                using (IIdentityService service = CreateService())
                {
                    TestCredentials credentials = _configuration.TryGetSelectedCredentials();
                    Assert.IsNotNull(credentials);

                    AuthenticationRequest request = credentials.AuthenticationRequest;
                    Assert.IsNotNull(request);

                    AuthenticateApiCall apiCall = await service.PrepareAuthenticateAsync(request, cancellationTokenSource.Token);
                    Tuple<HttpResponseMessage, AccessResponse> response = await apiCall.SendAsync(cancellationTokenSource.Token);

                    Assert.IsNotNull(response);
                    Assert.IsNotNull(response.Item2);

                    Access access = response.Item2.Access;
                    Assert.IsNotNull(access);
                    Assert.IsNotNull(access.Token);
                    Assert.IsNotNull(access.ServiceCatalog);
                    Assert.IsNotNull(access.User);

                    // check the token
                    Token token = access.Token;
                    Assert.IsNotNull(token);
                    Assert.IsNotNull(token.Id);
                    Assert.IsNotNull(token.Tenant);

                    // Rackspace does not return this property, and it doesn't seem to be particularly useful.
                    //Assert.IsNotNull(token.IssuedAt);

                    Assert.IsNotNull(token.ExpiresAt);

                    // check the user
                    User user = access.User;
                    Assert.IsNotNull(user);
                    Assert.IsNotNull(user.Id);
                    Assert.IsNotNull(user.Name);

                    // If the Username is null, it's presumed to be the same as the Name. (Rackspace does not return
                    // this property.)
                    //Assert.IsNotNull(user.Username);

                    Assert.IsNotNull(user.Roles);

                    Assert.AreNotEqual(0, user.Roles.Count);
                    foreach (Role role in user.Roles)
                    {
                        Assert.IsNotNull(role);
                        Assert.IsNotNull(role.Name);
                        Assert.AreNotEqual(string.Empty, role.Name);
                    }

                    // At least Rackspace does not return the roles_links property.
                    if (user.RolesLinks != null)
                    {
                        foreach (Link link in user.RolesLinks)
                        {
                            Assert.IsNotNull(link);
                            Assert.IsNotNull(link.Relation);
                            Assert.AreNotEqual(string.Empty, link.Relation);
                            Assert.IsNotNull(link.Target);
                            Assert.IsTrue(link.Target.IsAbsoluteUri);
                        }
                    }

                    // check the service catalog
                    ReadOnlyCollection<ServiceCatalogEntry> serviceCatalog = access.ServiceCatalog;
                    Assert.IsNotNull(serviceCatalog);
                    Assert.AreNotEqual(0, serviceCatalog.Count);
                    foreach (ServiceCatalogEntry entry in serviceCatalog)
                    {
                        Assert.IsNotNull(entry);
                        Assert.IsNotNull(entry.Name);
                        Assert.IsNotNull(entry.Type);
                        Assert.IsNotNull(entry.Endpoints);

                        Assert.AreNotEqual(0, entry.Endpoints.Count);
                        foreach (Endpoint endpoint in entry.Endpoints)
                        {
                            Assert.IsNotNull(endpoint);

                            // At least Rackspace does not return an ID with their endpoints. The ID doesn't seem
                            // necessary for API calls.
                            //Assert.IsNotNull(endpoint.Id);

                            // Region-independent endpoints may have a null Region value.
                            //Assert.IsNotNull(endpoint.Region);

                            Assert.IsFalse(endpoint.PublicUrl == null && endpoint.InternalUrl == null && endpoint.AdminUrl == null);
                        }

                        // At least Rackspace does not return the endpoints_links property.
                        if (entry.EndpointsLinks != null)
                        {
                            foreach (Link link in entry.EndpointsLinks)
                            {
                                Assert.IsNotNull(link);
                                Assert.IsNotNull(link.Relation);
                                Assert.AreNotEqual(string.Empty, link.Relation);
                                Assert.IsNotNull(link.Target);
                                Assert.IsTrue(link.Target.IsAbsoluteUri);
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public async Task TestListTenants()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TestTimeout(TimeSpan.FromSeconds(10)));

                TestCredentials credentials = _configuration.TryGetSelectedCredentials();
                Assert.IsNotNull(credentials);

                AuthenticationRequest request = credentials.AuthenticationRequest;
                Assert.IsNotNull(request);

                IdentityV2AuthenticationService authenticationService = new IdentityV2AuthenticationService(CreateService(), request);

                using (IIdentityService service = CreateService(authenticationService))
                {
                    ListTenantsApiCall apiCall = await service.PrepareListTenantsAsync(cancellationTokenSource.Token);
                    Tuple<HttpResponseMessage, ReadOnlyCollectionPage<Tenant>> response = await apiCall.SendAsync(cancellationTokenSource.Token);

                    Assert.IsNotNull(response);
                    Assert.IsNotNull(response.Item2);

                    ReadOnlyCollectionPage<Tenant> tenants = response.Item2;
                    Assert.IsNotNull(tenants);
                    Assert.AreNotEqual(0, tenants.Count);
                    Assert.IsFalse(tenants.CanHaveNextPage);

                    foreach (Tenant tenant in tenants)
                        CheckTenant(tenant);

                    Assert.IsTrue(tenants.Any(i => i.Enabled ?? false));
                }
            }
        }

        protected void CheckExtension(Extension extension)
        {
            Assert.IsNotNull(extension);
            Assert.IsNotNull(extension.Alias);
            Assert.IsNotNull(extension.Description);
            Assert.IsNotNull(extension.Name);
            Assert.IsNotNull(extension.Namespace);
            try
            {
                Assert.IsNotNull(extension.LastModified);
            }
            catch (JsonException)
            {
                // OpenStack is known to return an unrecognized (and undefined) timestamp format for this
                // property...
            }
        }

        protected void CheckTenant(Tenant tenant)
        {
            Assert.IsNotNull(tenant);
            Assert.IsNotNull(tenant.Id);
            Assert.IsNotNull(tenant.Name);
            Assert.IsFalse(string.IsNullOrEmpty(tenant.Name));

            // Caller should check for at least one enabled Tenant
            //Assert.AreEqual(true, tenant.Enabled);

            // At least Rackspace does not send back this property.
            //Assert.IsNotNull(tenant.Description);
            //Assert.IsFalse(string.IsNullOrEmpty(tenant.Description));
        }

        protected TimeSpan TestTimeout(TimeSpan timeSpan)
        {
            if (Debugger.IsAttached)
                return TimeSpan.FromDays(6);

            return timeSpan;
        }

        protected IIdentityService CreateService()
        {
            IdentityClient client;
            switch (Vendor)
            {
            case "HP":
                // currently HP does not have a vendor-specific IIdentityService
                goto default;

            case "Rackspace":
                client = new RackspaceIdentityClient(BaseAddress);
                break;

            case "OpenStack":
            default:
                client = new IdentityClient(BaseAddress);
                break;
            }

            TestProxy.ConfigureService(client, Proxy);
            client.BeforeAsyncWebRequest += TestHelpers.HandleBeforeAsyncWebRequest;
            client.AfterAsyncWebResponse += TestHelpers.HandleAfterAsyncWebResponse;

            return client;
        }

        protected IIdentityService CreateService(IAuthenticationService authenticationService)
        {
            IdentityClient client;
            switch (Vendor)
            {
            case "HP":
                // currently HP does not have a vendor-specific IIdentityService
                goto default;

            case "Rackspace":
                client = new RackspaceIdentityClient(authenticationService, BaseAddress);
                break;

            case "OpenStack":
            default:
                client = new IdentityClient(authenticationService, BaseAddress);
                break;
            }

            TestProxy.ConfigureService(client, Proxy);
            client.BeforeAsyncWebRequest += TestHelpers.HandleBeforeAsyncWebRequest;
            client.AfterAsyncWebResponse += TestHelpers.HandleAfterAsyncWebResponse;

            return client;
        }
    }
}
