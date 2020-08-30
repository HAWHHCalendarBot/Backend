#!/bin/sh
set -e

docker build -t hawhhcalendarbot/downloader:1 -f Dockerfile.Downloader .
docker build -t hawhhcalendarbot/parser:1 -f Dockerfile.Parser .
