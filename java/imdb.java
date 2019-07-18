import java.util.List;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Set;
import java.util.HashSet;
import java.util.TreeSet;
import java.util.Map;
import java.util.HashMap;
import java.util.TreeMap;
import java.nio.file.Paths;
import java.nio.file.Files;


class IMDB {
  static int counter;

  public static void main(String[] args) {
    try {
      if (args.length == 3) {
        String option = args[0];
        int repetitions = Integer.parseInt(args[1]);
        String path = args[2];

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
        else {
          printUsage();
        }
      }
      else {
        printUsage();
      }
    }
    catch (Exception e) {
      e.printStackTrace();
      System.out.println();
      printUsage();
    }
  }

  static void printUsage() {
    System.out.println("Usage: java -jar imdb-java.jar [-l|-u|-q|-uq] <repetitions> <input directory>");
    System.out.println("  -l   load dataset only");
    System.out.println("  -u   run updates");
    System.out.println("  -q   run queries");
    System.out.println("  -uq  run queries on updated dataset\n");
  }

  static void runTests(String path, int numOfQueryRuns, boolean runUpdates) throws Exception {
    MoviesDB moviesDB = new MoviesDB();

    boolean noQueries = numOfQueryRuns == 0;

    readMovies(moviesDB, path, false, noQueries ? 5 : 0);
    readActors(moviesDB, path, true, noQueries ? 5 : 0);
    readDirectors(moviesDB, path, true, noQueries ? 5 : 0);
    readMoviesDirectors(moviesDB, path, true, noQueries ? 5 : 0);
    readMoviesGenres(moviesDB, path, true, noQueries ? 5 : 0);
    readRoles(moviesDB, path, true, noQueries ? 5 : 0);

    if (!runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          System.out.print("\n");
        runQueries(moviesDB);
      }

    if (runUpdates) {
      bumpUpRankOfMoviesMadeInOrBefore(moviesDB, new int[] {1970, 1989, 2000},
                                        new double[] {0.2, 0.05, 0.05}, true, noQueries ? 6 : 0);

      calcActorsAvgsMoviesRanks(moviesDB, true, noQueries ? 4 : 0);
      calcDirectorsAvgsMoviesRanks(moviesDB, true, noQueries ? 4 : 0);

      bumpUpRankOfMovieAndAllItsActorsAndDirectors(moviesDB, 0.1, true, noQueries ? 5 : 0);

      deleteMoviesWithRankBelow(moviesDB, 4.0, true, noQueries ? 5 : 0);
      deleteActorsWithNoRoles(moviesDB, true, noQueries ? 4 : 0);
      deleteDirectorsWithNoMovies(moviesDB, true, noQueries ? 4 : 0);
    }

    if (runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          System.out.print("\n");
        runQueries(moviesDB);
      }

    System.out.println();
  }

  static void runQueries(MoviesDB moviesDB) throws Exception {
    numOfMoviesWithRankAbove(moviesDB, false, 4);
    numOfActorsWhoPlayedInAMovieWithRankAbove(moviesDB, true, 5);
    coActorsInMoviesWithRankAbove(moviesDB, true, 5);
    moviesAgeHistogram(moviesDB, true, 4);
    avgAgeOfMoviesWithRankAbove(moviesDB, true, 4);
    sumOfAllMoviesAges(moviesDB, true, 4);

    moviesWithActorsInCommon(moviesDB, true, 5);
    uniqueLastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 5);

    coActorsWithCountInMoviesWithRankAbove(moviesDB, true, 5);
    lastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 5);
    isAlsoActor(moviesDB, true, 4);
    fullName(moviesDB, true, 4);
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

      moviesDB.addMovie(id, name, year, rank);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readActors(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/actors.csv"));

    long msecs1 = System.currentTimeMillis();

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

      Actor.Gender gender;
      if (genderStr.equals("M"))
        gender = Actor.Gender.male;
      else if (genderStr.equals("F"))
        gender = Actor.Gender.female;
      else
        throw new RuntimeException();

      moviesDB.addActor(id, firstName, lastName, gender);
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

      moviesDB.addDirector(id, firstName, lastName);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readMoviesDirectors(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/movies_directors.csv"));

    long msecs1 = System.currentTimeMillis();

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int directorId = (int) reader.readLong();
      reader.skip(';');
      int movieId = (int) reader.readLong();
      reader.skipLine();

      moviesDB.addMovieDirector(directorId, movieId);
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
      String genre = reader.readString();
      reader.skipLine();

      moviesDB.addMovieGenre(movieId, genresMap.get(genre));
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void readRoles(MoviesDB moviesDB, String path, boolean printSep, int width) throws Exception {
    byte[] content = Files.readAllBytes(Paths.get(path + "/roles.csv"));

    long msecs1 = System.currentTimeMillis();

    CsvReader reader = new CsvReader(content);
    reader.skipLine();
    while (!reader.eof()) {
      int actorId = (int) reader.readLong();
      reader.skip(';');
      int movieId = (int) reader.readLong();
      reader.skip(';');
      String role = reader.readString();
      reader.skipLine();

      moviesDB.addRole(actorId, movieId, role);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void bumpUpRankOfMoviesMadeInOrBefore(MoviesDB moviesDB, int[] years, double[] factors, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    for (int i=0 ; i < years.length ; i++)
      moviesDB.bumpUpRankOfMoviesMadeInOrBefore(years[i], factors[i]);
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void calcActorsAvgsMoviesRanks(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    moviesDB.calcActorsAvgsMoviesRanks();
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void calcDirectorsAvgsMoviesRanks(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    moviesDB.calcDirectorsAvgsMoviesRanks();
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void bumpUpRankOfMovieAndAllItsActorsAndDirectors(MoviesDB moviesDB, double factor, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxId = 0;
    for (Movie m : moviesDB.movies.values())
      if (m.id > maxId)
        maxId = m.id;
    int numOfIds = moviesDB.movies.size() / 4;
    int[] randomIds = randomInts(maxId, numOfIds, 735025);

    for (int id : randomIds) {
      Movie movie = moviesDB.movies.get(id);
      if (movie != null)
        moviesDB.bumpUpRankOfMovieAndAllItsActorsAndDirectors(movie, factor);
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void deleteMoviesWithRankBelow(MoviesDB moviesDB, double minRank, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    moviesDB.deleteMoviesWithRankBelow(minRank);
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void deleteActorsWithNoRoles(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    moviesDB.deleteActorsWithNoRoles();
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void deleteDirectorsWithNoMovies(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();
    moviesDB.deleteDirectorsWithNoMovies();
    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void moviesWithActorsInCommon(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxMovieId = maxMovieId();
    int numOfIds = moviesDB.movies.size() / 6;
    int[] randomIds = randomInts(maxMovieId, numOfIds, 64798);

    long count = 0;
    int misses = 0;

    for (int id : randomIds) {
      Movie movie = moviesDB.movies.get(id);
      if (movie != null) {
        Set<Movie> movies = movie.moviesWithActorsInCommon();
        count += movies.size();
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void moviesAgeHistogram(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int[] histogram;
    for (int i=0 ; i < 50 ; i++)
      histogram = moviesDB.moviesAgeHistogram(1900, 5.0 + i * 0.1);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void isAlsoActor(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long count = 0;
    long otherCount = 0;
    for (Director d : moviesDB.directors.values()) {
      if (moviesDB.isAlsoActor(d))
        count++;
      else
        otherCount++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void fullName(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long len = 0;
    for (Actor a : moviesDB.actors.values()) {
      String fullName = a.fullName();
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
      counts[i] = moviesDB.numOfMoviesWithRankAbove((i+1) * 0.1);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void numOfActorsWhoPlayedInAMovieWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int[] counts = new int[50];
    for (int i=0 ; i < 50 ; i++)
      counts[i] = moviesDB.numOfActorsWhoPlayedInAMovieWithRankAbove((i+1) * 0.2);

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void coActorsInMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    long maxCoActors = 0;

    for (Actor a : moviesDB.actors.values()) {
      // Actor[] coActors = moviesDB.coActorsInMoviesWithRankAbove(a, 6.0);
      // if (coActors.length > maxCoActors)
      //   maxCoActors = coActors.length;
      Set<Actor> coActors = moviesDB.coActorsInMoviesWithRankAbove(a, 6.0);
      if (coActors.size() > maxCoActors)
        maxCoActors = coActors.size();
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void coActorsWithCountInMoviesWithRankAbove(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxActorId = maxActorId();
    int numOfIds = moviesDB.actors.size() / 4;
    int[] randomIds = randomInts(maxActorId, numOfIds, 72594);

    long maxCoActors = 0;
    int misses = 0;

    for (int id : randomIds) {
      Actor actor = moviesDB.actors.get(id);
      if (actor != null) {
        Map<Actor, Integer> coActors = moviesDB.coActorsWithCountInMoviesWithRankAbove(actor, 6.0);
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

    int maxActorId = maxActorId();
    int numOfIds = moviesDB.actors.size() / 10;
    int[] randomIds = randomInts(maxActorId, numOfIds, 47619);

    int maxNo = 0;
    int misses = 0;

    for (int id : randomIds) {
      Actor actor = moviesDB.actors.get(id);
      if (actor != null) {
        List<String> actors = moviesDB.lastNamesOfActorsWithSameFirstNameAs(actor);
        if (actors.size() > maxNo)
          maxNo = actors.size();
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  static void uniqueLastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, boolean printSep, int width) {
    long msecs1 = System.currentTimeMillis();

    int maxActorId = maxActorId();
    int numOfIds = moviesDB.actors.size() / 20;
    int[] randomIds = randomInts(maxActorId, numOfIds, 35102);

    int maxNo = 0;
    int misses = 0;

    for (int id : randomIds) {
      Actor actor = moviesDB.actors.get(id);
      if (actor != null) {
        Set<String> actors = moviesDB.uniqueLastNamesOfActorsWithSameFirstNameAs(actor);
        if (actors.size() > maxNo)
          maxNo = actors.size();
      }
      else
        misses++;
    }

    long msecs2 = System.currentTimeMillis();
    printTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////

  int maxActorId() {
    int maxId = 0;
    for (Actor a : moviesDB.actors.values())
      if (a.id > maxId)
        maxId = a.id;
    return maxId;
  }

  int maxMovieId() {
    int maxId = 0;
    for (Movie m : moviesDB.movies.values())
      if (m.id > maxId)
        maxId = m.id;
    return maxId;
  }

  //////////////////////////////////////////////////////////////////////////////

  static Map<String, Movie.Genre> genresMap;

  static {
    genresMap = new HashMap<String, Movie.Genre>();
    genresMap.put("Action",       Movie.Genre.action);
    genresMap.put("Adult",        Movie.Genre.adult);
    genresMap.put("Adventure",    Movie.Genre.adventure);
    genresMap.put("Animation",    Movie.Genre.animation);
    genresMap.put("Comedy",       Movie.Genre.comedy);
    genresMap.put("Crime",        Movie.Genre.crime);
    genresMap.put("Documentary",  Movie.Genre.documentary);
    genresMap.put("Drama",        Movie.Genre.drama);
    genresMap.put("Family",       Movie.Genre.family);
    genresMap.put("Fantasy",      Movie.Genre.fantasy);
    genresMap.put("Film-Noir",    Movie.Genre.filmNoir);
    genresMap.put("Horror",       Movie.Genre.horror);
    genresMap.put("Music",        Movie.Genre.music);
    genresMap.put("Musical",      Movie.Genre.musical);
    genresMap.put("Mystery",      Movie.Genre.mystery);
    genresMap.put("Romance",      Movie.Genre.romance);
    genresMap.put("Sci-Fi",       Movie.Genre.sciFi);
    genresMap.put("Short",        Movie.Genre._short);
    genresMap.put("Thriller",     Movie.Genre.thriller);
    genresMap.put("War",          Movie.Genre.war);
    genresMap.put("Western",      Movie.Genre.western);
  }

  //////////////////////////////////////////////////////////////////////////////

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

////////////////////////////////////////////////////////////////////////////////

class Movie {
  public enum Genre {
    action,
    adult,
    adventure,
    animation,
    comedy,
    crime,
    documentary,
    drama,
    family,
    fantasy,
    filmNoir,
    horror,
    music,
    musical,
    mystery,
    romance,
    sciFi,
    _short,
    thriller,
    war,
    western
  };

  public int    id;
  public String name;
  public int    year;
  public double rank;

  public ArrayList<Genre> genres = new ArrayList<Genre>();
  public ArrayList<Role>  actors = new ArrayList<Role>();
  public ArrayList<Director> directors = new ArrayList<Director>();

  public Movie(int id, String name, int year, double rank) {
    this.id = id;
    this.name = name;
    this.year = year;
    this.rank = rank;
  }

  public void add(Genre genre) {
    genres.add(genre);
  }

  public void add(Role role) {
    actors.add(role);
  }

  public void add(Director director) {
    directors.add(director);
  }

  public int age(int currYear) {
    return currYear - year;
  }

  public Set<Movie> moviesWithActorsInCommon() {
    Set<Movie> movies = new HashSet<Movie>();
    for (Role r1 : actors)
      for (Role r2 : r1.actor.roles)
        if (r2.movie != this)
          movies.add(r2.movie);
    return movies;
  }
}


class Actor {
  public enum Gender {male, female};

  public int    id;
  public String firstName;
  public String lastName;
  public Gender gender;
  public double avgMoviesRank = -1.0;

  public ArrayList<Role> roles = new ArrayList<Role>();

  public Actor(int id, String firstName, String lastName, Gender gender) {
    this.id = id;
    this.firstName = firstName;
    this.lastName = lastName;
    this.gender = gender;
  }

  public void add(Role role) {
    roles.add(role);
  }

  public void remove(Role role) {
    roles.remove(role);
  }

  public String fullName() {
    return firstName + " " + lastName;
  }

  public void calcAvgsMoviesRank() {
    int count = 0;
    double sum = 0.0;
    for (Role r : roles) {
      double rank = r.movie.rank;
      if (rank > 0) {
        count++;
        sum += rank;
      }
    }
    if (count > 0)
      avgMoviesRank = sum / count;
  }
}


class Role {
  public Movie  movie;
  public Actor  actor;
  public String role;

  public Role(Movie movie, Actor actor, String role) {
    this.movie = movie;
    this.actor = actor;
    this.role = role;
    movie.add(this);
    actor.add(this);
  }
}


class Director {
  public int    id;
  public String firstName;
  public String lastName;
  public double avgMoviesRank = -1.0;

  public ArrayList<Movie> movies = new ArrayList<Movie>();

  public Director(int id, String firstName, String lastName) {
    this.id = id;
    this.firstName = firstName;
    this.lastName = lastName;
  }

  public void add(Movie movie) {
    movies.add(movie);
    movie.add(this);
  }

  public void remove(Movie movie) {
    movies.remove(movie);
  }

  public void calcAvgsMoviesRank() {
    int count = 0;
    double sum = 0.0;
    for (Movie m : movies)
      if (m.rank > 0) {
        count++;
        sum += m.rank;
      }
    if (count > 0)
      avgMoviesRank = sum / count;
  }
}


class MoviesDB {
  public Map<Integer, Movie> movies = new HashMap<Integer, Movie>();
  public Map<Integer, Actor> actors = new HashMap<Integer, Actor>();
  public Map<Integer, Director> directors = new HashMap<Integer, Director>();

  public Map<String, Set<Actor>> actorsByFirstName = new HashMap<String, Set<Actor>>();
  public Map<String, Set<Actor>> actorsByLastName = new HashMap<String, Set<Actor>>();


  public void addMovie(int id, String name, int year, double rank) {
    movies.put(id, new Movie(id, name, year, rank));
  }

  public void addActor(int id, String firstName, String lastName, Actor.Gender gender) {
    Actor actor = new Actor(id, firstName, lastName, gender);
    actors.put(id, actor);

    Set<Actor> sameNameActors = actorsByFirstName.get(firstName);
    if (sameNameActors == null) {
      sameNameActors = new HashSet<Actor>();
      actorsByFirstName.put(firstName, sameNameActors);
    }
    sameNameActors.add(actor);

    sameNameActors = actorsByLastName.get(lastName);
    if (sameNameActors == null) {
      sameNameActors = new HashSet<Actor>();
      actorsByLastName.put(lastName, sameNameActors);
    }
    sameNameActors.add(actor);
  }

  public void addDirector(int id, String firstName, String lastName) {
    directors.put(id, new Director(id, firstName, lastName));
  }

  public void addMovieGenre(int movieId, Movie.Genre genre) {
    movies.get(movieId).add(genre);
  }

  public void addMovieDirector(int directorId, int movieId) {
    directors.get(directorId).add(movies.get(movieId));
  }

  public void addRole(int actorId, int movieId, String roleDescr) {
    new Role(movies.get(movieId), actors.get(actorId), roleDescr);
  }

  //////////////////////////////////////////////////////////////////////////////

  public void bumpUpRankOfMoviesMadeInOrBefore(int year, double factor) {
    for (Movie m : movies.values())
      if (m.year <= year)
        m.rank += factor * (10.0 - m.rank);
  }

  //////////////////////////////////////////////////////////////////////////////

  public void calcActorsAvgsMoviesRanks() {
    for (Actor a : actors.values())
      a.calcAvgsMoviesRank();
  }

  public void calcDirectorsAvgsMoviesRanks() {
    for (Director d : directors.values())
      d.calcAvgsMoviesRank();
  }

  //////////////////////////////////////////////////////////////////////////////

  public void bumpUpRankOfMovieAndAllItsActorsAndDirectors(Movie movie, double factor) {
    double delta = factor * (10.0 - movie.rank);
    movie.rank += delta;

    for (Role r : movie.actors) {
      Actor actor = r.actor;
      if (actor.avgMoviesRank > 0.0) {
        int count = actor.roles.size();
        actor.avgMoviesRank += delta / count;
      }
    }

    for (Director d : movie.directors)
      if (d.avgMoviesRank > 0.0) {
        int count = d.movies.size();
        d.avgMoviesRank += delta / count;
      }
  }

  public void deleteMoviesWithRankBelow(double rank) {
    ArrayList<Movie> moviesToRemove = new ArrayList<Movie>();
    for (Movie m : movies.values())
      if (m.rank < rank)
        moviesToRemove.add(m);
    for (Movie m : moviesToRemove) {
      Movie remMovie = movies.remove(m.id);
      if (remMovie != m)
        throw new RuntimeException();
      for (Role r : m.actors)
        r.actor.remove(r);
      for (Director d : m.directors)
        d.remove(m);
    }
  }

  public void deleteActorsWithNoRoles() {
    ArrayList<Actor> actorsToRemove = new ArrayList<Actor>();
    for (Actor a : actors.values())
      if (a.roles.isEmpty())
        actorsToRemove.add(a);
    for (Actor a : actorsToRemove) {
      Actor remActor = actors.remove(a.id);
      if (remActor != a)
        throw new RuntimeException();
    }
  }

  public void deleteDirectorsWithNoMovies() {
    ArrayList<Director> directorsToRemove = new ArrayList<Director>();
    for (Director d : directors.values())
      if (d.movies.isEmpty())
        directorsToRemove.add(d);
    for (Director d : directorsToRemove) {
      Director remDirector = directors.remove(d.id);
      if (remDirector != d)
        throw new RuntimeException();
    }
  }

  //////////////////////////////////////////////////////////////////////////////

  public int numOfMoviesWithRankAbove(double minRank) {
    int count = 0;
    for (Movie movie : movies.values())
      if (movie.rank >= minRank)
        count++;
    return count;
  }

  public int numOfActorsWhoPlayedInAMovieWithRankAbove(double minRank) {
    int count = 0;
    for (Actor actor : actors.values())
      for (Role role : actor.roles)
        if (role.movie.rank >= minRank) {
          count++;
          break;
        }
    return count;
  }

  // public Actor[] coActorsInMoviesWithRankAbove(Actor actor, double minRank) {
  public Set<Actor> coActorsInMoviesWithRankAbove(Actor actor, double minRank) {
    HashSet<Actor> coActors = new HashSet<Actor>();
    // TreeSet<Actor> coActors = new TreeSet<Actor>((a1, a2) -> a1.id - a2.id);
    for (Role r1 : actor.roles)
      if (r1.movie.rank >= minRank)
        for (Role r2 : r1.movie.actors)
          if (r2.actor != actor)
            coActors.add(r2.actor);
    // return coActors.toArray(new Actor[0]);
    return coActors;
  }

  public Map<Actor, Integer> coActorsWithCountInMoviesWithRankAbove(Actor actor, double minRank) {
    HashMap<Actor, Integer> coActors = new HashMap<Actor, Integer>();
    // TreeMap<Actor, Integer> coActors = new TreeMap<Actor, Integer>((a1, a2) -> a1.id - a2.id);
    for (Role r1 : actor.roles)
      if (r1.movie.rank >= minRank)
        for (Role r2 : r1.movie.actors)
          if (r2.actor != actor)
            coActors.put(r2.actor, coActors.getOrDefault(r2.actor, 0));
    return coActors;
  }

  public List<String> lastNamesOfActorsWithSameFirstNameAs(Actor actor) {
    Set<Actor> actors = actorsByFirstName.get(actor.firstName);
    List<String> lastNames = new ArrayList<String>();
    if (actors != null)
      for (Actor a : actors)
        if (a != actor)
          lastNames.add(a.lastName);
    return lastNames;
  }

  public Set<String> uniqueLastNamesOfActorsWithSameFirstNameAs(Actor actor) {
    Set<Actor> actors = actorsByFirstName.get(actor.firstName);
    Set<String> lastNames = new HashSet<String>();
    if (actors != null)
      for (Actor a : actors)
        if (a != actor)
          lastNames.add(a.lastName);
    return lastNames;
  }

  public boolean isAlsoActor(Director director) {
    Set<Actor> actors = actorsByLastName.get(director.lastName);
    if (actors != null)
      for (Actor a : actors)
        if (a.firstName.equals(director.firstName))
          return true;
    return false;
  }

  public int[] moviesAgeHistogram(int startYear, double minRank) {
    int[] histogram = new int[0];
    for (Movie m : movies.values())
      if (m.rank >= minRank && m.year >= startYear) {
        int idx = m.year - startYear;
        if (idx >= histogram.length)
          histogram = Arrays.copyOf(histogram, idx+1);
        histogram[idx]++;
      }
    return histogram;
  }

  public double avgAgeOfMoviesWithRankAbove(int currYear, double minRank) {
    long totalAge = 0;
    int count = 0;
    for (Movie m : movies.values()) {
      if (m.rank >= minRank) {
        int age = m.age(currYear);
        totalAge += age;
        count++;
      }
    }
    return totalAge / (double) count;
  }

  public long sumOfAllMoviesAges(int currYear) {
    long totalAge = 0;
    for (Movie m : movies.values()) {
      int age = m.age(currYear);
      totalAge += age;
    }
    return totalAge;
  }
}
