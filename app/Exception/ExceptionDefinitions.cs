namespace App.Exceptions;

public class BadRequestException(string message) : Exception(message);

public class UnauthorizedException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class InternalServerException(string message) : Exception(message);