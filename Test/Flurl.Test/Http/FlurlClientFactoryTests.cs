﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using NUnit.Framework;

namespace Flurl.Test.Http
{
	[TestFixture]
    public class FlurlClientFactoryTests
    {
	    [Test]
	    public void per_host_factory_provides_same_client_per_host() {
		    var fac = new PerHostFlurlClientFactory();
		    var cli1 = fac.Get("http://api.com/foo");
		    var cli2 = fac.Get("https://api.com/bar");
		    Assert.AreSame(cli1, cli2);
	    }

	    [Test]
	    public void per_base_url_factory_provides_same_client_per_provided_url() {
		    var fac = new PerBaseUrlFlurlClientFactory();
		    var cli1 = fac.Get("http://api.com/foo");
		    var cli2 = fac.Get("http://api.com/bar");
		    var cli3 = fac.Get("http://api.com/foo");
		    Assert.AreNotSame(cli1, cli2);
		    Assert.AreSame(cli1, cli3);
	    }

	    [Test]
	    public void can_configure_client_from_factory() {
		    var fac = new PerHostFlurlClientFactory()
			    .ConfigureClient("http://api.com/foo", c => c.Settings.CookiesEnabled = true);
		    Assert.IsTrue(fac.Get("https://api.com/bar").Settings.CookiesEnabled);
		    Assert.IsFalse(fac.Get("http://api2.com/foo").Settings.CookiesEnabled);
	    }

	    private static object _lock = new object();

	    [Test]
	    public void can_multithread_on_appveyor() {
		    var sequence = new List<int>();

		    var task = Task.Run(() => {
			    lock (_lock) {
				    Thread.Sleep(2000);
				    sequence.Add(2);
			    }
		    });
		    sequence.Add(1);
		    task.Wait();

		    Assert.AreEqual("1,2", string.Join(",", sequence));
		}

		[Test, Ignore("troubleshooting")]
	    public async Task ConfigureClient_is_thread_safe() {
		    var fac = new PerHostFlurlClientFactory();

		    var sequence = new List<int>();

		    var task1 = Task.Run(() => fac.ConfigureClient("http://api.com", c => {
			    sequence.Add(1);
				Thread.Sleep(200);
			    sequence.Add(3);
		    }));

		    await Task.Delay(50);

		    sequence.Add(2);

			// modifies same client as task1, should get blocked until task1 is done
		    var task2 = Task.Run(() => fac.ConfigureClient("http://api.com", c => {
			    sequence.Add(4);
		    }));

			//await Task.Delay(50);
		    //var task3 = Task.Run(() => fac.ConfigureClient("http://api2.com", c => {
			//	sequence.Add(2);
		    //}));

			await Task.WhenAll(task1, task2);
		    Assert.AreEqual("1,2,3,4", string.Join(",", sequence));
	    }
	}
}
