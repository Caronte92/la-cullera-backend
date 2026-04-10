// <copyright file="ChangePasswordValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
  public ChangePasswordValidator()
  {
    this.RuleFor(x => x.currentPassword)
        .NotEmpty().WithMessage("Current password is required");

    this.RuleFor(x => x.newPassword)
        .NotEmpty().WithMessage("New password is required")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
        .MaximumLength(72).WithMessage("Password must not exceed 72 characters")
        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
        .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
        .Matches("[0-9]").WithMessage("Password must contain at least one number")
        .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
  }
}
