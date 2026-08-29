using FluentValidation;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Application.Models;

namespace NotesAndFileBackend.Api.Validators;

public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Summary)
            .MaximumLength(1000).WithMessage("Summary cannot exceed 1000 characters.");
    }
}

public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Summary)
            .MaximumLength(1000).WithMessage("Summary cannot exceed 1000 characters.");
    }
}

public class CreateShareRequestValidator : AbstractValidator<CreateShareRequest>
{
    public CreateShareRequestValidator()
    {
        RuleFor(x => x.Alias)
            .MaximumLength(100).WithMessage("Alias cannot exceed 100 characters.")
            .Matches("^[a-zA-Z0-9_-]*$").WithMessage("Alias can only contain alphanumeric characters, dashes, and underscores.");
    }
}
