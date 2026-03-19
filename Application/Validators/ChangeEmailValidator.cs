// <copyright file="ChangeEmailValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class ChangeEmailValidator : AbstractValidator<ChangeEmailDto>
{
  public ChangeEmailValidator()
  {
    this.RuleFor(x => x.newEmail)
        .NotEmpty().WithMessage("Email is required")
        .EmailAddress().WithMessage("Invalid email format")
        .MaximumLength(255);
  }
}
