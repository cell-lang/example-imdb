imdb.jar: cell/main.cell cell/imdb.cell cell/csv.cell
	@rm -rf imdb.jar tmp/gen tmp/net/
	@mkdir -p tmp/gen/
	java -jar bin/cellc-java.jar projects/standalone.txt tmp/gen/
	javac -g -d tmp/ tmp/gen/*.java
	jar cfe imdb.jar net.cell_lang.Generated -C tmp net/

imdb-java.jar: java/imdb.java
	@rm -rf imdb-java.jar tmp/java
	@mkdir -p tmp/java
	javac -d tmp/java/ java/imdb.java
	jar cfe imdb-java.jar IMDB -C tmp/java /

imdb.dll: csharp/imdb.cs csharp/imdb.csproj
	@rm -rf imdb.dll csharp/bin csharp/obj
	cd csharp ; dotnet build -c Release
	ln -s csharp/bin/Release/netcoreapp2.2/imdb.dll .

imdb-embedded.jar: cell/main.java cell/imdb.cell cell/csv.cell cell/embedded.cell
	@rm -rf imdb-embedded.jar tmp/gen tmp/net/
	@mkdir -p tmp/gen/
	java -jar bin/cellc-java.jar projects/embedded.txt tmp/gen/
	javac -g -d tmp/ cell/main.java tmp/gen/*.java
	jar cfe imdb-embedded.jar IMDB -C tmp net/ `cd tmp/ ; ls *.class | sed 's/^/ -C tmp /'`

clean:
	@rm -rf tmp/* imdb.jar imdb-embedded.jar imdb-java.jar imdb.dll csharp/bin csharp/obj
