(docker exec -i RIN.WebAPI.DB pg_dump -U tmwadmin -s TMW --schema=webapi) > webapi_schema.sql