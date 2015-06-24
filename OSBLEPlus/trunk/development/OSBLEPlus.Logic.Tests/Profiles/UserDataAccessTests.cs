using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLEPlus.Logic.DataAccess.Profiles;


namespace OSBLEPlus.Logic.Tests.Profiles
{
    [TestClass]
    public class UserDataAccessTests
    {
        [TestMethod]
        public void TestGetUserById()
        {
            // Arrange, using seeded admin
            var userId = 1;

            // Act
            var user = UserDataAccess.GetById(userId);

            // Assert
            Assert.IsNotNull(user);
            Assert.IsTrue(user.UserId == userId);
            Assert.IsTrue(user.IsAdmin);
        }

        [TestMethod]
        public void TestGetUserByName()
        {
            // Arrange, using seeded admin
            var userName = UserDataAccess.GetById(1).Email;

            // Act
            var user = UserDataAccess.GetByName(userName);

            // Assert
            Assert.IsNotNull(user);
            Assert.IsTrue(user.Email == userName);
        }

        [TestMethod]
        public void TestValidateUser()
        {
            // Arrange, using seeded admin
            var userName = UserDataAccess.GetById(1).Email;

            // Act
            var result = UserDataAccess.ValidateUser(userName, "123123");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestLogUserTransaction()
        {
            // Arrange, using seeded admin
            var user = UserDataAccess.GetById(1);

            // Act
            var result = UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);

            // Assert
            Assert.IsTrue(result == -1);
        }
    }
}
