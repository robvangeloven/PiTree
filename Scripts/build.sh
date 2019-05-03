#!/bin/bash

git pull git@github.com:robvangeloven/PiTree.git
docker build -t PiTree -f Docker.arm32 .