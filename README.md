# RIN.WebAPI
Web API server for the client

## Setup
* Download and install [Docker](https://www.docker.com/get-started).
    * Local development will most likely want [Docker Desktop](https://hub.docker.com/editions/community/docker-ce-desktop-windows).
* Run `docker compose -p RIN_WebAPI up` from the `docker` folder to start the dev stack.
* Run [SDBBrowser](https://github.com/themeldingwars/SDBrowser) and load an `sdb` file.
* Click `DB` and enter the connection string:
    * `User ID=tmwadmin;Password=change;Host=localhost;Port=5434;Database=TMW;`
* Click `Import` and you should see the log importing the data, wait for it to finish.
* Go to the `sql` folder and run `ImportSchema.bat`
* Open `http://localhost:8081/browser` in your browser and sign in to the postgres admin with the email `tmwlocaldev@tmwlocaldev.net` and password `tmwlocaldev`
    * These are for local development only, **CHANGE THEM** if you deploy this.
* In the DB you should see 2 schemas `sdb` and `webapi`
    * The `public` schema can be ignored.

## Running RIN
* Compile the latest changes to the `RIN.WebAPI` solution.
* Run the project as `RIN.WEBAPI` or `RIN.WebAPI CMD`.
    * The difference between these two profiles is `CMD` will only launch RIN while the other profile will also launch Swagger (`https://localhost:5001/swagger/index.html`) in your browser for checking endpoints.
    * When prompted about trusting the `ASP.NET Core SSL Certificate`, select `Yes`.
        * It is possible you will need to restart RIN/Docker/Firefall after installing the certificate for them to properly regcognize it.
* You will most likely not need to restart `Docker` unless you make DB Schema changes.

## Contributing / Updating
If you have made db changes run `sql/ExportSchema.bat` and check in the `webapi_schema.sql` file to make sure the schema changes are shared.

## Tech / libs used
* RIN is built on [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
