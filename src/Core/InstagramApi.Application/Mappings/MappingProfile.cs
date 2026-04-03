using AutoMapper;
using InstagramApi.Application.DTOs.Admin;
using InstagramApi.Application.DTOs.Auth;
using InstagramApi.Application.DTOs.Comment;
using InstagramApi.Application.DTOs.Message;
using InstagramApi.Application.DTOs.Notification;
using InstagramApi.Application.DTOs.Post;
using InstagramApi.Application.DTOs.Story;
using InstagramApi.Application.DTOs.User;
using InstagramApi.Domain.Entities;

namespace InstagramApi.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<AppUser, UserDto>()
            .ForMember(d => d.IsFollowing, o => o.Ignore())
            .ForMember(d => d.IsFollowedBy, o => o.Ignore())
            .ForMember(d => d.IsBlocked, o => o.Ignore());

        CreateMap<AppUser, UserSummaryDto>()
            .ForMember(d => d.IsFollowing, o => o.Ignore());

        CreateMap<AppUser, UserProfileDto>()
            .ForMember(d => d.IsFollowing, o => o.Ignore())
            .ForMember(d => d.IsFollowedBy, o => o.Ignore())
            .ForMember(d => d.IsBlocked, o => o.Ignore())
            .ForMember(d => d.Roles, o => o.Ignore());

        CreateMap<AppUser, AdminUserDto>()
            .ForMember(d => d.Roles, o => o.Ignore());

        // Post
        CreateMap<Post, PostDto>()
            .ForMember(d => d.IsLiked, o => o.Ignore())
            .ForMember(d => d.IsSaved, o => o.Ignore())
            .ForMember(d => d.Hashtags, o => o.MapFrom(s =>
                s.PostHashtags.Select(ph => ph.Hashtag.Name).ToList()))
            .ForMember(d => d.TaggedUsers, o => o.MapFrom(s =>
                s.Tags.Select(t => t.TaggedUser).ToList()));

        CreateMap<PostMedia, PostMediaDto>();

        // Comment
        CreateMap<Comment, CommentDto>()
            .ForMember(d => d.IsLiked, o => o.Ignore());

        // Story
        CreateMap<Story, StoryDto>()
            .ForMember(d => d.IsViewed, o => o.Ignore());

        // Message
        CreateMap<Message, MessageDto>()
            .ForMember(d => d.ReplyToMessage, o => o.Ignore());

        CreateMap<Conversation, ConversationDto>()
            .ForMember(d => d.Participants, o => o.MapFrom(s =>
                s.Participants.Select(p => p.User).ToList()))
            .ForMember(d => d.LastMessage, o => o.Ignore())
            .ForMember(d => d.UnreadCount, o => o.Ignore());

        // Notification
        CreateMap<Notification, NotificationDto>();

        // Report
        CreateMap<Report, ReportDto>();
    }
}
