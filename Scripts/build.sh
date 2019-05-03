#!/bin/bash

git pull git@github.com:robvangeloven/PiTree.git
docker image build -t robvangeloven/pi-tree -f Docker.arm32 .