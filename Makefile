all : init

init:
	docker build -t data-cleaner -f Dockerfile .
	docker create --name data-cleaner-container -p 8080:80 data-cleaner

serve:
	docker start data-cleaner-container

test: 
	curl -k --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0001", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse
	curl -k --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0002", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse
	curl -k --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0003", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse
