import java.util.List;
import java.util.ArrayList;
import java.util.Set;
import java.util.Map;
import java.util.HashMap;
import java.io.FileReader;
import java.io.FileWriter;
import java.nio.file.Paths;
import java.nio.file.Files;

import net.cell_lang.*;


class IMDB {
  static int counter;

  public static void main(String[] args) throws Exception {
    int repetitions;

    if (args.length == 3 | args.length == 4) {
      try {
        repetitions = Integer.parseInt(args[1]);
      }
      catch (Exception e) {
        printUsage();
        System.out.println();
        throw e;
      }

      String option = args[0];
      String path = args[2];

      if (args.length == 4) {
        String outFile = args[3];

        if (option.equals("-w")) {
          runStoringTest(path, outFile, false, repetitions);
        }
        else if (option.equals("-uw")) {
          runStoringTest(path, outFile, true, repetitions);
        }
        else {
          printUsage();
        }
        return;
      }

      if (option.equals("-l")) {
        for (int i=0 ; i < repetitions ; i++)
          runTests(path, 0, false);
      }
      else if (option.equals("-u")) {
        for (int i=0 ; i < repetitions ; i++)
          runTests(path, 0, true);
      }
      else if (option.equals("-q")) {
        runTests(path, repetitions, false);
      }
      else if (option.equals("-uq")) {
        runTests(path, repetitions, true);
      }
      else if (option.equals("-r")) {
        for (int i=0 ; i < repetitions ; i++)
          runLoadingTests(path);
      }
      else {
        printUsage();
      }
    }
    else {
      printUsage();
    }
  }

  static void printUsage() {
    System.out.println("Usage: java -jar imdb-embedded.jar [-l|-u|-q|-uq] <repetitions> <input directory>");
    System.out.println("  -l   load dataset from csv files");
    System.out.println("  -u   run updates");
    System.out.println("  -q   run queries");
    System.out.println("  -uq  run queries on updated dataset");
    System.out.println();
    System.out.println("or: java -jar imdb-embedded.jar [-w|-uw] <repetitions> <input directory> <output file>");
    System.out.println("  -w   load dataset and write state to specified output file");
    System.out.println("  -uw  load dataset and write updated state to specified output file");
    System.out.println();
    System.out.println("or: java -jar imdb-embedded.jar [-r] <repetitions> <input file>");
    System.out.println("  -r   read a previously saved (with the -w or -uw options) state");
  }

  static void runLoadingTests(String inputFile) throws Exception {
    MoviesDB moviesDB = new MoviesDB();

    long msecs1 = System.currentTimeMillis();

    try (FileReader reader = new FileReader(inputFile)) {
      moviesDB.load(reader);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, false, 6);
    System.out.println();
  }

  static void runStoringTest(String inputPath, String outputFile, boolean runUpdates, int repetitions) throws Exception {
    MoviesDB moviesDB = new MoviesDB();

    readCsvFiles(moviesDB, inputPath, false);

    if (runUpdates)
      runUpdates(moviesDB, false);

    for (int i=0 ; i < repetitions ; i++) {
      long msecs1 = System.currentTimeMillis();

      String file = outputFile;
      if (repetitions > 1 && file.endsWith(".txt")) {
        file = file.substring(0, file.length() - 4) + String.format("-%02d", i) + ".txt";
      }

      try (FileWriter writer = new FileWriter(file)) {
        moviesDB.save(writer);
      }

      long msecs2 = System.currentTimeMillis();
      printTime(msecs2 - msecs1, false, 6);
      System.out.println();
    }
  }

  static void runTests(String path, int numOfQueryRuns, boolean runUpdates) throws Exception {
    MoviesDB moviesDB = new MoviesDB();

    boolean noQueries = numOfQueryRuns == 0;

    readCsvFiles(moviesDB, path, noQueries);

    if (!runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          System.out.print("\n");
        runQueries(moviesDB);
      }

    if (runUpdates)
      runUpdates(moviesDB, noQueries);

    if (runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          System.out.print("\n");
        runQueries(moviesDB);
      }

    System.out.println();
  }

  static void readCsvFiles(MoviesDB moviesDB, String path, boolean printTimes) throws Exception {
    readMovies(moviesDB, path, false, printTimes ? 5 : 0);
    readActors(moviesDB, path, true, printTimes ? 5 : 0);
    readDirectors(moviesDB, path, true, printTimes ? 5 : 0);
    readMoviesDirectors(moviesDB, path, true, printTimes ? 5 : 0);
    readMoviesGenres(moviesDB, path, true, printTimes ? 5 : 0);
    readRoles(moviesDB, path, true, printTimes ? 6 : 0);
  }

  static void runUpdates(MoviesDB moviesDB, boolean printTimes) {
    bumpUpRankOfMoviesMadeInOrBefore(moviesDB, new int[] {1970, 1989, 2000},
                                      new double[] {0.2, 0.05, 0.05}, true, printTimes ? 6 : 0);

    calcActorsAvgsMoviesRanks(moviesDB, true, printTimes ? 4 : 0);
    calcDirectorsAvgsMoviesRanks(moviesDB, true, printTimes ? 4 : 0);

    bumpUpRankOfMovieAndAllItsActorsAndDirectors(moviesDB, 0.1, true, printTimes ? 5 : 0);

    deleteMoviesWithRankBelow(moviesDB, 4.0, true, printTimes ? 5 : 0);
    deleteActorsWithNoRoles(moviesDB, true, printTimes ? 4 : 0);
    deleteDirectorsWithNoMovies(moviesDB, true, printTimes ? 4 : 0);
  }

  static void runQueries(MoviesDB moviesDB) throws Exception {
    numOfMoviesWithRankAbove(moviesDB, false, 4);
    numOfActorsWhoPlayedInAMovieWithRankAbove(moviesDB, true, 6);
    coActorsInMoviesWithRankAbove(moviesDB, true, 5);
    moviesAgeHistogram(moviesDB, true, 3);
    avgAgeOfMoviesWithRankAbove(moviesDB, true, 3);
    sumOfAllMoviesAges(moviesDB, true, 3);

    moviesWithActorsInCommon(moviesDB, true, 6);
    uniqueLastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 6);

    coActorsWithCountInMoviesWithRankAbove(moviesDB, true, 6);
    lastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 7);
    isAlsoActor(moviesDB, true, 4);
    directorsWhoAreAlsoActors(moviesDB, false, 5);
    fullName(moviesDB, true, 5);
  }

  //////////////////////////////////////////////////////////////////////////////

  static void readMovies(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/movies.csv"));

    long msecs1 = System.currentTimeMillis();

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int id = (int) reader.readLong();
      reader.skip(';');
      String name = reader.readString();
      reader.skip(';');
      int year = (int) reader.readLong();
      reader.skip(';');
      double rank = reader.readDouble();
      reader.skipLine();

      String msg = "add_movie(id: " + Integer.toString(id) + ", name: \"" + escape(name) + "\", year: " +
                   Integer.toString(year) + ", rank: " + Double.toString(rank) + ", genres: [])";

      moviesDB.execute(msg);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readActors(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/actors.csv"));

    long msecs1 = System.currentTimeMillis();

    boolean indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int id = (int) reader.readLong();
      reader.skip(';');
      String firstName = reader.readString();
      reader.skip(';');
      String lastName = reader.readString();
      reader.skip(';');
      String genderStr = reader.readString();
      reader.skipLine();

      String gender;
      if (genderStr.equals("M"))
        gender = "male";
      else if (genderStr.equals("F"))
        gender = "female";
      else
        throw new RuntimeException();

      String msg = "add_actor(id: " + Integer.toString(id) + ", first_name: \"" + escape(firstName) +
                   "\", last_name: \"" + escape(lastName) + "\", gender: " + gender + ")";

      moviesDB.execute(msg);

      if (!indexCreationTriggered) {
        moviesDB.actorsByFirstName("...");
        moviesDB.actorsByLastName("...");
        indexCreationTriggered = true;
      }
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readDirectors(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/directors.csv"));

    long msecs1 = System.currentTimeMillis();

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int id = (int) reader.readLong();
      reader.skip(';');
      String firstName = reader.readString();
      reader.skip(';');
      String lastName = reader.readString();
      reader.skipLine();

      String msg = "add_director(id: " + Integer.toString(id) + ", first_name: \"" +
                   escape(firstName) + "\", last_name: \"" + escape(lastName) + "\")";

      moviesDB.execute(msg);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readMoviesDirectors(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/movies_directors.csv"));

    long msecs1 = System.currentTimeMillis();

    boolean indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int directorId = (int) reader.readLong();
      reader.skip(';');
      int movieId = (int) reader.readLong();
      reader.skipLine();

      String msg = "add_movie_director(movie_id: " + Integer.toString(movieId) +
                   ", director_id: " + Integer.toString(directorId) + ")";

      moviesDB.execute(msg);

      if (!indexCreationTriggered) {
        moviesDB.directorsOf(0);
        indexCreationTriggered = true;
      }
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readMoviesGenres(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/movies_genres.csv"));

    long msecs1 = System.currentTimeMillis();

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int movieId = (int) reader.readLong();
      reader.skip(';');
      String genreStr = reader.readString();
      reader.skipLine();

      String genre = genresMap.get(genreStr);

      String msg = "add_movie_genre(id: " + Integer.toString(movieId) + ", genre: " + genre + ")";

      moviesDB.execute(msg);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readRoles(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/roles.csv"));

    long msecs1 = System.currentTimeMillis();

    boolean indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int actorId = (int) reader.readLong();
      reader.skip(';');
      int movieId = (int) reader.readLong();
      reader.skip(';');
      String role = reader.readString();
      reader.skipLine();

      String roleField = role.length() > 0 ? ", role: \"" + escape(role) + "\"" : "";

      String msg = "add_movie_actor(movie_id: " + Integer.toString(movieId) + ", actor_id: " +
                   Integer.toString(actorId) + roleField + ")";

      moviesDB.execute(msg);

      if (!indexCreationTriggered) {
        moviesDB.cast(0);
        indexCreationTriggered = true;
      }
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void bumpUpRankOfMoviesMadeInOrBefore(MoviesDB moviesDB, int[] years, double[] factors, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    for (int i=0 ; i < years.length ; i++) {
      String msg = "bump_up_rank_of_movies_made_in_or_before(year: " + Integer.toString(years[i]) +
                   ", factor: " + Double.toString(factors[i]) + ")";

      moviesDB.execute(msg);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void calcActorsAvgsMoviesRanks(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    moviesDB.execute("calc_actor_avg_movies_rank");

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void calcDirectorsAvgsMoviesRanks(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    moviesDB.execute("calc_director_avg_movies_rank");

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void bumpUpRankOfMovieAndAllItsActorsAndDirectors(MoviesDB moviesDB, double factor, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxId = (int) moviesDB.maxMovieId();
    int numOfIds = (int) moviesDB.numOfMovies() / 4;
    int[] randomIds = randomInts(maxId, numOfIds, 735025);

    for (int id : randomIds) {
      if (moviesDB.movieExists(id)) {
        String msg = "bump_up_rank_of_movie_and_its_actors_and_directors(movie: movie(" +
                     Integer.toString(id) + "), factor: " + Double.toString(factor) + ")";

        moviesDB.execute(msg);
      }
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void deleteMoviesWithRankBelow(MoviesDB moviesDB, double minRank, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    String msg = "delete_movies_with_rank_below(min_rank: " + Double.toString(minRank) + ")";
    moviesDB.execute(msg);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void deleteActorsWithNoRoles(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    moviesDB.execute("delete_actors_with_no_roles");

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void deleteDirectorsWithNoMovies(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    moviesDB.execute("delete_directors_with_no_movies");

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void moviesWithActorsInCommon(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxId = (int) moviesDB.maxMovieId();
    int numOfIds = (int) moviesDB.numOfMovies() / 6;
    int[] randomIds = randomInts(maxId, numOfIds, 64798);

    long count = 0;
    int misses = 0;

    for (int id : randomIds) {
      if (moviesDB.movieExists(id)) {
        long[] movies = moviesDB.moviesWithActorsInCommon(id);
        count += movies.length;
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void moviesAgeHistogram(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long[] histogram;
    for (int i=0 ; i < 50 ; i++)
      histogram = moviesDB.movieAgeHistogram(1900, 5.0 + i * 0.1);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void isAlsoActor(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long count = 0;
    long otherCount = 0;
    for (long id : moviesDB.directors()) {
      if (moviesDB.isAlsoActor(id))
        count++;
      else
        otherCount++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void directorsWhoAreAlsoActors(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long[] directorsActors = moviesDB.directorsWhoAreAlsoActors();
    if (directorsActors.length == 0)
      throw new RuntimeException();

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void fullName(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long len = 0;
    for (long id : moviesDB.actors()) {
      String fullName = moviesDB.fullName(id);
      len += fullName.length();
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void avgAgeOfMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    double avgAge;
    for (int i=0 ; i < 50 ; i++)
      avgAge = moviesDB.avgAgeOfMoviesWithRankAbove(2019, 5.0 + i * 0.1);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void sumOfAllMoviesAges(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long totalAge;
    for (int i=0 ; i < 10 ; i++)
      for (int year=2019 ; year < 2040 ; year++)
        totalAge = moviesDB.sumOfAllMoviesAges(year);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void numOfMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int[] counts = new int[100];
    for (int i=0 ; i < 100 ; i++)
      counts[i] = (int) moviesDB.numOfMoviesWithRankAbove((i+1) * 0.1);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void numOfActorsWhoPlayedInAMovieWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int[] counts = new int[50];
    for (int i=0 ; i < 50 ; i++)
      counts[i] = (int) moviesDB.numOfActorsWhoPlayedInAMovieWithRankAbove((i+1) * 0.2);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void coActorsInMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long maxCoActors = 0;
    for (long id : moviesDB.actors()) {
      long[] coActors = moviesDB.coActorsInMoviesWithRankAbove(id, 6.0);
      if (coActors.length > maxCoActors)
        maxCoActors = coActors.length;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void coActorsWithCountInMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxId = (int) moviesDB.maxActorId();
    int numOfIds = (int) moviesDB.numOfActors() / 4;
    int[] randomIds = randomInts(maxId, numOfIds, 72594);

    long maxCoActors = 0;
    int misses = 0;

    for (int id : randomIds) {
      if (moviesDB.actorExists(id)) {
        Value coActors = moviesDB.coActorsWithCountInMoviesWithRankAbove(id, 6.0);
        if (coActors.size() > maxCoActors)
          maxCoActors = coActors.size();
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void lastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxId = (int) moviesDB.maxActorId();
    int numOfIds = (int) moviesDB.numOfActors() / 10;
    int[] randomIds = randomInts(maxId, numOfIds, 47619);

    int maxNo = 0;
    int misses = 0;

    for (int id : randomIds) {
      if (moviesDB.actorExists(id)) {
        String[] actors = moviesDB.lastNamesOfActorsWithSameFirstNameAs(id);
        if (actors.length > maxNo)
          maxNo = actors.length;
      }
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void uniqueLastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxActorId = (int) moviesDB.maxActorId();
    int numOfIds = (int) moviesDB.numOfActors() / 20;
    int[] randomIds = randomInts(maxActorId, numOfIds, 35102);

    int maxNo = 0;
    int misses = 0;

    for (int id : randomIds) {
      if (moviesDB.actorExists(id)) {
        String[] actors = moviesDB.uniqueLastNamesOfActorsWithSameFirstNameAs(id);
        if (actors.length > maxNo)
          maxNo = actors.length;
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////

  static Map<String, String> genresMap;

  static {
    genresMap = new HashMap<String, String>();
    genresMap.put("Action",       "action");
    genresMap.put("Adult",        "adult");
    genresMap.put("Adventure",    "adventure");
    genresMap.put("Animation",    "animation");
    genresMap.put("Comedy",       "comedy");
    genresMap.put("Crime",        "crime");
    genresMap.put("Documentary",  "documentary");
    genresMap.put("Drama",        "drama");
    genresMap.put("Family",       "family");
    genresMap.put("Fantasy",      "fantasy");
    genresMap.put("Film-Noir",    "film_noir");
    genresMap.put("Horror",       "horror");
    genresMap.put("Music",        "music");
    genresMap.put("Musical",      "musical");
    genresMap.put("Mystery",      "mystery");
    genresMap.put("Romance",      "romance");
    genresMap.put("Sci-Fi",       "sci_fi");
    genresMap.put("Short",        "short");
    genresMap.put("Thriller",     "thriller");
    genresMap.put("War",          "war");
    genresMap.put("Western",      "western");
  }

  //////////////////////////////////////////////////////////////////////////////

  static String escape(String str) {
    return str.replace("\\", "\\\\").replace("\"", "\\\"");
  }

  static void printTime(long time, boolean printSep, int width) {
    if (width > 0) {
      if (printSep)
        System.out.print(",");
      String str = Long.toString(time);
      while (str.length() < width)
        str = " " + str;
      System.out.print(str);
    }
  }

  static int[] randomInts(int max, int count, int seed) {
    final long m = 2147483648L;
    final long a = 1103515245;
    final long c = 12345;

    long state = seed;
    int[] ints = new int[count];
    for (int i=0 ; i < count ; i++) {
      state = (a * state + c) % m;
      ints[i] = (int) (state % (max + 1));
    }
    return ints;
  }
}

////////////////////////////////////////////////////////////////////////////////

class CsvReader {
  byte[] content;
  int    index;

  public CsvReader(byte[] content) {
    this.content = content;
    index = 0;
  }

  public void skip(char ch) {
    if (!nextIs(ch))
      throw new RuntimeException();
    read();
  }

  public void skipLine() {
    while (!eof() && read() != '\n')
      ;
  }

  public List<Object> readRow() {
    List<Object> row = new ArrayList<Object>();
    for ( ; ; ) {
      row.add(readField());
      if (eof())
        return row;
      if (nextIs('\n')) {
        read();
        return row;
      }
      if (!nextIs(';')) {
        System.out.printf("peek() = %s, row = %s\n", peek(), row);
      }
      check(nextIs(';'));
      read();
    }
  }

  Object readField() {
    if (Character.isDigit(peek()))
      return readNumber();
    else if (nextIs('"'))
      return readString();
    else
      throw new RuntimeException();
  }

  long readLong() {
    boolean neg = nextIs('-');
    if (neg)
      read();
    check(Character.isDigit(peek()));
    long value = read() - '0';
    while (!eof() && Character.isDigit(peek()))
      value = 10 * value + read() - '0';
    return neg ? -value : value;
  }

  Double readDouble() {
    boolean neg = nextIs('-');
    if (neg)
      read();
    check(Character.isDigit(peek()));
    double value = read() - '0';
    while (!eof() && Character.isDigit(peek()))
      value = 10 * value + read() - '0';
    if (eof() || !nextIs('.'))
      return neg ? -value : value;
    read();
    double weigth = 0.1;
    while (!eof() && Character.isDigit(peek())) {
      value += weigth * (read() - '0');
      weigth = 0.1 * weigth;
    }
    return value;
  }

  Number readNumber() {
    boolean neg = nextIs('-');
    if (neg)
      read();
    check(Character.isDigit(peek()));
    long value = read() - '0';
    while (!eof() && Character.isDigit(peek()))
      value = 10 * value + read() - '0';
    if (eof() || !nextIs('.'))
      return neg ? -value : value;
    read();
    double floatValue = value;
    double digitValue = 0.1;
    while (!eof() && Character.isDigit(peek())) {
      floatValue += digitValue * (read() - '0');
      digitValue = 0.1 * digitValue;
    }
    return floatValue;
  }

  String readString() {
    StringBuilder sb = new StringBuilder();
    check(nextIs('"'));
    read();
    for ( ; ; ) {
      char ch = read();
      if (ch == '"')
        if (!nextIs('"'))
          return sb.toString();
        else
          read();
      sb.append(ch);
    }
  }

  char read() {
    return (char) content[index++];
  }

  char peek() {
    return (char) content[index];
  }

  boolean nextIs(char ch) {
    return index < content.length && content[index] == ch;
  }

  boolean eof() {
    return index >= content.length;
  }

  void check(boolean cond) {
    if (!cond)
      throw new RuntimeException();
  }
}