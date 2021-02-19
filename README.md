# [Cosmos CMS (Editor)](https://cosmos.azureedge.net)

[![License: State of California](https://img.shields.io/static/v1?label=License&message=Cosmos%20CMS%20(Editor)&color=brightgreen)](https://github.com/StateOfCalifornia/CosmosCMS.Editor/edit/main/LICENSE.md)

_*Please Note:* Each Cosmos CMS website is actually made up of two parts--and two repositories. This repository is for the "Editor." This application is used to create and edit web content.  The other is the ["Publisher"](https://github.com/StateOfCalifornia/CosmosCMS.Publisher), and it is the forward-facing website that hosts the website that the public sees._

Cosmos CMS (C/CMS) is a [ASP.NET Core (v.5)](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-5.0?view=aspnetcore-5.0) hybrid Web Content Management System (CMS). It is a "hybrid" because of it's extremely open architecture allows for you to [mashup](https://en.wikipedia.org/wiki/Mashup_(web_application_hybrid)) this CMS with your own functionality and integrations.

## Cloud-first Design

C/CMS is built for [the cloud](https://cosmos.azureedge.net/), and makes use of services such as:

* Redis cache
* Content Distribution Networks 
  * Akamai
  * Microsoft
  * Verizon
* Blob storage for assets
* Google Translate (v3)
* OAuth
  * Google
  * Microsoft

C/CMS also takes advantage of the cloud's ability to automatically scale, and, run simultaneously in multiple regions.


