# Let's Encrypt Azure

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fsjkp%2Fletsencrypt-azure%2Fmaster%2Fsrc%2FLetsEncrypt.Azure.ResourceGroup%2FTemplates%2Fletsencrypt.functionapp.renewer.json" target="_blank"><img src="http://azuredeploy.net/deploybutton.png"/></a>

# What is Let's Encrypt Azure

Let's Encrypt Azure is my second attempt to bring Let's Encrypt certificates to Azure. My first attempt letsencrypt-siteextension was merely a prototype, that I refined a little and 
shared with other people so they could also benefit from getting Let's Encrypt certificates for their Azure Web Apps. 

While the original project worked, it had a few shortcomings. Let's Encrypt Azure is an attempt to fix those while using an architecture 
that will allow Let's Encrypt Azure to be used for other services in Azure, not just for Azure Web Apps. 

Some of the shortcommings that Let's Encrypt Azure is trying to fix are:

* Easier installation (letsencrypt-siteextension was to many a configuration nightmare, this time around I'm going to aim for the default scenario, setting up letsencrypt certificates on a single azure web app, to be a one-click process, with a more validation)
* Better renewal (The renewal process of the original extension was handled in a Web Job running inside the web app, while it did work, it had some serious flaws, e.g. you would not get notifications when it failed, depending on how you deployed you web app, you could accidentially delete the job). 
* Better support for larger environments (My original extension was really only thought through for the single web app scenario, this time around I will try to prepare my solution for multi-region deploys with traffic manager, and people using web slots)
* Better security (instead of storing certificates inside the Azure Web app, this time around we are going to leverage Azure Key Vault)
