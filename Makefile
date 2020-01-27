all : init

init:
	docker build -t data-cleaner -f Dockerfile .
	docker create --name data-cleaner-container -p 8080:80 data-cleaner

serve:
	docker start data-cleaner-container

test: 
	@echo "\nTest Id 0001 is Obfuscated"
	@curl -k --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0001", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse
	@echo "\nTest Id 0002 is Not Obfuscated"
	@curl -k --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0002", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse
	@echo "\nTest correct response on inner api fail"
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0003", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 504; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id not string. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": 1, "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 400; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id to short. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": "1", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 400; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id to long. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": "10000", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 400; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id to lower limit. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": "0001", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 200; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id to upper limit. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "id": "9999", "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 200; then echo "pass"; else echo "fail"; fi
	@echo "\nTest validation id missing. "
	@if curl -k -s -o /dev/null -w "%{http_code}" --header "Content-Type: application/json" -d '{"row-id": 1, "fred": "fred"}' -X POST http://localhost:8080/datacleaner/cleanse | grep 400; then echo "pass"; else echo "fail"; fi
