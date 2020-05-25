#!/bin/sh
set -e

docker build -t hawhhcalendarbotdownloader -f Dockerfile.Downloader .
docker build -t hawhhcalendarbotparser -f Dockerfile.Parser .
