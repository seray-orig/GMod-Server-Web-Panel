using GMServerWebPanel.API.Data;
using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services;
using GMServerWebPanel.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GMServerWebPanel.API.UnitTests
{
    /*
    public class UserServiceTests
    {
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly UserService _userService;
        private readonly Mock<DbSet<User>> _usersDbSetMock;
        private readonly Mock<AppDbContext> _dbContextMock;

        public UserServiceTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _usersDbSetMock = new Mock<DbSet<User>>();
            _dbContextMock = new Mock<AppDbContext>();

            // Настройка контекста для работы с DbSet<User>
            _dbContextMock.Setup(c => c.Users).Returns(_usersDbSetMock.Object);

            _userService = new UserService(_dbContextMock.Object, _passwordHasherMock.Object);
        }

        [Fact]
        public void GetUserBy_WhenLoginOrPasswordIsEmpty_ReturnsNull()
        {
            // Arrange
            string emptyLogin = string.Empty;
            string validPassword = "password";
            string validLogin = "user";
            string emptyPassword = string.Empty;

            // Act & Assert
            Assert.Null(_userService.GetUserBy(emptyLogin, validPassword));
            Assert.Null(_userService.GetUserBy(validLogin, emptyPassword));
            Assert.Null(_userService.GetUserBy(null!, validPassword));
            Assert.Null(_userService.GetUserBy(validLogin, null!));
        }

        [Fact]
        public void GetUserBy_WhenUserNotFound_ReturnsNull()
        {
            // Arrange
            string login = "nullUser";
            string password = "any";

            var users = new List<User>().AsQueryable();
            SetupUsersDbSet(users);

            // Act
            var result = _userService.GetUserBy(login, password);

            // Assert
            Assert.Null(result);
            _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetUserBy_WhenPasswordIsInvalid_ReturnsNull()
        {
            // Arrange
            string login = "validUser";
            string wrongPassword = "wrong";
            var user = new User { Login = login, Password = "hashedPassword" };

            var users = new List<User> { user }.AsQueryable();
            SetupUsersDbSet(users);

            _passwordHasherMock
                .Setup(h => h.VerifyPassword(wrongPassword, user.Password))
                .Returns(false);

            // Act
            var result = _userService.GetUserBy(login, wrongPassword);

            // Assert
            Assert.Null(result);
            _passwordHasherMock.Verify(h => h.VerifyPassword(wrongPassword, user.Password), Times.Once);
        }

        [Fact]
        public void GetUserBy_WhenCredentialsAreValid_ReturnsUser()
        {
            // Arrange
            string login = "validUser";
            string correctPassword = "correct";
            var user = new User { Login = login, Password = "hashedPassword" };

            var users = new List<User> { user }.AsQueryable();
            SetupUsersDbSet(users);

            _passwordHasherMock
                .Setup(h => h.VerifyPassword(correctPassword, user.Password))
                .Returns(true);

            // Act
            var result = _userService.GetUserBy(login, correctPassword);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(login, result.Login);
            _passwordHasherMock.Verify(h => h.VerifyPassword(correctPassword, user.Password), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WhenUserIsValid_HashesPasswordAndSaves()
        {
            // Arrange
            var user = new User { Login = "newUser", Password = "plainPassword" };
            string hashedPassword = "hashedPassword";

            _passwordHasherMock
                .Setup(h => h.HashPassword(user.Password))
                .Returns(hashedPassword);

            _dbContextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _userService.CreateUserAsync(user);

            // Assert
            Assert.True(result);
            Assert.Equal(hashedPassword, user.Password); // Проверяем, что пароль захеширован
            _usersDbSetMock.Verify(s => s.Add(user), Times.Once);
            _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WhenDbUpdateExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var user = new User { Login = "duplicateUser", Password = "plain" };
            string hashedPassword = "hashed";

            _passwordHasherMock
                .Setup(h => h.HashPassword(user.Password))
                .Returns(hashedPassword);

            _dbContextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException());

            // Act
            var result = await _userService.CreateUserAsync(user);

            // Assert
            Assert.False(result);
            _usersDbSetMock.Verify(s => s.Add(user), Times.Once);
            _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Настраивает мок DbSet<User> для поддержки IQueryable операций (FirstOrDefault).
        /// </summary>
        private void SetupUsersDbSet(IQueryable<User> users)
        {
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());
        }
    }
    */
}
