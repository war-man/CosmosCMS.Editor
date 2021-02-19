# [Cosmos CMS (Editor)](https://cosmos.azureedge.net)

[![License: State of California](https://img.shields.io/static/v1?label=License&message=Cosmos%20CMS%20(Editor)&color=brightgreen)](https://github.com/StateOfCalifornia/CosmosCMS.Editor/edit/main/LICENSE.md) [![Build Status](https://dev.azure.com/CalEnterprise/CDT.Cosmos.Cms/_apis/build/status/Source-GitHub%20CosmosCMS.Editor?branchName=main)](https://dev.azure.com/CalEnterprise/CDT.Cosmos.Cms/_build/latest?definitionId=474&branchName=main) [![Board Status](https://dev.azure.com/CalEnterprise/a7ab809f-6843-401d-962e-130106405388/dcd608b7-7c08-4e48-8863-83d649e2e1df/_apis/work/boardbadge/82ea9a1e-2fcd-4973-8898-080c0556e997)](https://dev.azure.com/calenterprise/a7ab809f-6843-401d-962e-130106405388/_boards/board/t/dcd608b7-7c08-4e48-8863-83d649e2e1df/Microsoft.RequirementCategory/)

Cosmos CMS (C/CMS) is a ASP.NET Core (v.5) hybrid [Web Content Management System](https://en.wikipedia.org/wiki/Web_content_management_system). It is a "[hybrid](https://en.wikipedia.org/wiki/Mashup_(web_application_hybrid))" because of it's extremely open architecture that allows for you to mashup this CMS with your own functionality and integrations.

Each Cosmos CMS website is actually made up of two parts--and two repositories stored in GitHub:

* The "[Editor](https://github.com/StateOfCalifornia/CosmosCMS.Editor)" is the web application used to create content for a website.
* The "[Publisher](https://github.com/StateOfCalifornia/CosmosCMS.Publisher)" application is used to publish web content on the web.

Note: This repository is for the _*Editor*_.

This dual system ensures that the publishing website is unhindered with the overhead of the publishing logic and infrastructure, and it allows the publisher site to almost any kind of funtionality included in it.  

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

## Getting Started

This documentation is still under development, so check back for more topics as they become available.

* [Main documentation page](https://cosmos.azureedge.net/documentation)
* [Installation](https://cosmos.azureedge.net/installation)
* [Configuration](https://cosmos.azureedge.net/configuration)
* [Website Setup](https://cosmos.azureedge.net/website_setup)
* [Create and edit web pages](https://cosmos.azureedge.net/edit_page)
* [Web page versioning](https://cosmos.azureedge.net/page_versions)
* [Scheduling page publishing](https://cosmos.azureedge.net/page_versions#ScheduleRelease)
* [Product videos](https://cosmos.azureedge.net/video)
* [File management](https://cosmos.azureedge.net/file_management)
