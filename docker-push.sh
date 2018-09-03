#!/bin/bash

function tag {
  docker tag $1 edjopato/$1:latest
  docker push edjopato/$1:latest
}

tag hawhhcalendarbotdownloader
tag hawhhcalendarbotmensa
tag hawhhcalendarbotparser
