# PiTree
A Raspberry Pi powered, tree based, build status monitor

Tired of the same old ways to display your build status? Why not decorate a Christmas tree with a three different sets of Christmas lights and show your build status that way?

Raspberry Pi .NET Core based Docker image that integrates with Azure DevOps via either webhook or Azure Servicebus. Lights are controlled via the GPIO pins on the Raspberry Pi and in turn control a set of three relays.

A Docker image is available by pulling the Docker image via: `docker run robvangeloven/pi-tree:latest`

TODO:
- ~~Create Docker Hub image~~
- Create better config to switch between webhook, servicebus
- Add different light integrations (like Philips Hue) instead of only relay based switching
- Add unittests
