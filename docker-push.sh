#!/bin/bash

function tag {
  docker tag hawhhcalendarbot$1 hawhhcalendarbot/$1:latest
  docker push hawhhcalendarbot/$1:latest
}

tag downloader
tag mensa
tag parser
