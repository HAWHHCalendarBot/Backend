#!/bin/bash

docker build -t hawhhcalendardownloader -f Dockerfile.Downloader .
docker build -t hawhhcalendarmensa -f Dockerfile.Mensa .
docker build -t hawhhcalendarparser -f Dockerfile.Parser .
