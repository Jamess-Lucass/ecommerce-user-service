PROJECT_NAME=ecommerce-user-service

.PHONY: dev
dev:
	dotenv -- dotnet run --project ./src

.PHONY: migrate
migrate:
	dotenv -- dotnet ef database update --project ./src

.PHONY: migrate-add
migrate-add:
ifdef NAME
	dotenv -- dotnet ef migrations add $(NAME) --project ./src
else
	$(error Please supply the 'NAME' variable to name the migration, eg. 'NAME=example')
endif

.PHONY: seed
seed:
	dotenv -- dotnet run --project ./tests/seed

.PHONY: test
test:
	dotenv -- dotnet test ./tests/integration

.PHONY: coverage
coverage: 
	dotenv -- dotnet test ./tests/integration --collect:"XPlat Code Coverage" --results-directory:"./.coverage"
	reportgenerator -reports:".coverage/**/*.cobertura.xml" -targetdir:".coverage-report" -reporttypes:Html
	if [ $(shell docker ps -a | grep ${PROJECT_NAME}-coverage | wc -l ) -gt 0 ]; then docker stop ${PROJECT_NAME}-coverage; fi
	docker run --rm -d --name ${PROJECT_NAME}-coverage -p 9000:80 -v $(shell pwd)/.coverage-report:/usr/share/nginx/html nginx:alpine-slim

.PHONY: format
format:
	dotnet format ./src
	dotnet format ./tests/integration

.PHONY: compose
compose:
ifdef SERVICE
	docker compose up -d $(SERVICE)
else
	docker compose up -d
endif

.PHONY: compose-build
compose-build:
ifdef SERVICE
	docker compose up -d $(SERVICE) --build
else
	docker compose up -d --build
endif
