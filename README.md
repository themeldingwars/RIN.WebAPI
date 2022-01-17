# RIN.WebAPI
Web API server for the client

## Setup
* Download and install Docker.
* Run ``docker compose -p RIN_WebAPI up`` from the ``docker`` folder to start the dev stack.
* Open ``http://localhost:8081/browser`` in your browser and sign in to the postgred admin with the email ``tmwlocaldev@tmwlocaldev.net`` and password ``tmwlocaldev`` (these are for local dev only, if you deploy this CHANGE THEM)
* Run SDBBrowser and load an sdb file.
* * Click ``DB`` and enter the connection string ``User ID=tmwadmin;Password=change;Host=localhost;Port=5434;Database=TMW;``
* * Click ``Import`` and you should see the log importing the data, wait for it to finish.
* TODO import the db schema

## Running

## Tech / libs used
