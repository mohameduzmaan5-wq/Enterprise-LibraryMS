using System.Text.RegularExpressions;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Business logic service for Member operations.
/// </summary>
public class MemberService
{
    private readonly IMemberRepository _repository;

    public MemberService()
    {
        _repository = new MemberRepository();
    }

    public Task<IEnumerable<Member>> GetAllMembersAsync() => _repository.GetAllAsync();
    public Task<Member?> GetMemberByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<IEnumerable<Member>> SearchMembersAsync(string term) => _repository.SearchAsync(term);
    public Task<int> GetTotalMembersAsync() => _repository.GetTotalCountAsync();
    public Task<int> GetNewMembersThisMonthAsync() => _repository.GetNewThisMonthCountAsync();
    public Task<IEnumerable<Member>> GetActiveMembersAsync() => _repository.GetActiveMembersAsync();

    /// <summary>
    /// Adds a new member with validation.
    /// </summary>
    public async Task<(bool Success, string Message, int Id)> AddMemberAsync(Member member)
    {
        if (string.IsNullOrWhiteSpace(member.FirstName))
            return (false, "First name is required.", 0);
        if (string.IsNullOrWhiteSpace(member.LastName))
            return (false, "Last name is required.", 0);
        
        if (!string.IsNullOrWhiteSpace(member.Email) && !IsValidEmail(member.Email))
            return (false, "Please enter a valid email address.", 0);
            
        if (!string.IsNullOrWhiteSpace(member.Phone) && !IsValidPhone(member.Phone))
            return (false, "Please enter a valid phone number.", 0);

        var id = await _repository.AddAsync(member);
        return (true, "Member registered successfully!", id);
    }

    /// <summary>
    /// Updates an existing member with validation.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateMemberAsync(Member member)
    {
        if (member.Id <= 0)
            return (false, "Invalid Member ID.");
            
        if (string.IsNullOrWhiteSpace(member.FirstName))
            return (false, "First name is required.");
        if (string.IsNullOrWhiteSpace(member.LastName))
            return (false, "Last name is required.");

        if (!string.IsNullOrWhiteSpace(member.Email) && !IsValidEmail(member.Email))
            return (false, "Please enter a valid email address.");
            
        if (!string.IsNullOrWhiteSpace(member.Phone) && !IsValidPhone(member.Phone))
            return (false, "Please enter a valid phone number.");

        var result = await _repository.UpdateAsync(member);
        return result ? (true, "Member updated successfully!") : (false, "Failed to update member.");
    }

    /// <summary>
    /// Deletes a member by ID.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteMemberAsync(int id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Member removed successfully!") : (false, "Member not found.");
        }
        catch (Exception ex)
        {
            return (false, $"Cannot delete: This member may have active loans. {ex.Message}");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone, @"^[\+\d\-\s()]{7,20}$");
    }
}
