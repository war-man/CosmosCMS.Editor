# Cosmos CMS (Editor)

_*Please Note:* Each C/CMS websites is actually made up of two parts--and two repositories. This repository is for the "Editor." This application is used to create and edit web content.  The other is the ["Publisher"](https://github.com/StateOfCalifornia/CosmosCMS.Publisher). It is the forward-facing website that hosts the website that the public sees._

Cosmos CMS is a [Web Content Management System](https://en.wikipedia.org/wiki/Web_content_management_system) built with lessons-learned by the staff of the [California Department of Technology](https://cdt.ca.gov) with regards to building websites for state emergencies.

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
