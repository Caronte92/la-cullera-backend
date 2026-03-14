// <copyright file="HealthCheckTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Common;
using Application.Interfaces;
using Moq;

public class HealthServiceTests
{
  [Fact]
  public async Task CheckAsync_Returns_HealthStatus()
  {
    // Arrange
    var mockService = new Mock<IHealthCheckService>();
    var expectedStatus = new HealthStatus(apiUp: true, databaseUp: true);

    mockService
        .Setup(x => x.CheckAsync())
        .ReturnsAsync(expectedStatus);

    // Act
    var result = await mockService.Object.CheckAsync();

    // Assert
    Assert.NotNull(result);
    Assert.True(result.apiUp);
    Assert.True(result.databaseUp);
    mockService.Verify(x => x.CheckAsync(), Times.Once);
  }

  [Fact]
  public async Task CheckAsync_Returns_False_When_Database_Down()
  {
    // Arrange
    var mockService = new Mock<IHealthCheckService>();
    var expectedStatus = new HealthStatus(apiUp: true, databaseUp: false);

    mockService
        .Setup(x => x.CheckAsync())
        .ReturnsAsync(expectedStatus);

    // Act
    var result = await mockService.Object.CheckAsync();

    // Assert
    Assert.NotNull(result);
    Assert.True(result.apiUp);
    Assert.False(result.databaseUp);
  }
}
