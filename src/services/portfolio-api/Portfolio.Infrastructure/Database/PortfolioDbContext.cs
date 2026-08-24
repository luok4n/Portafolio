using Microsoft.EntityFrameworkCore;

namespace Portfolio.Infrastructure.Database;

/// <summary>
/// The portfolio schema. Configuration is written with the Fluent API rather than attributes so the
/// persistence rules stay in the persistence layer and the row classes remain plain.
/// </summary>
internal sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<ProfileRow> Profiles => Set<ProfileRow>();

    public DbSet<ProfileTranslationRow> ProfileTranslations => Set<ProfileTranslationRow>();

    public DbSet<SpokenLanguageRow> SpokenLanguages => Set<SpokenLanguageRow>();

    public DbSet<ExperienceRow> Experiences => Set<ExperienceRow>();

    public DbSet<ExperienceTranslationRow> ExperienceTranslations => Set<ExperienceTranslationRow>();

    public DbSet<ExperienceHighlightRow> ExperienceHighlights => Set<ExperienceHighlightRow>();

    public DbSet<ExperienceTechnologyRow> ExperienceTechnologies => Set<ExperienceTechnologyRow>();

    public DbSet<ExperienceTeamRow> ExperienceTeams => Set<ExperienceTeamRow>();

    public DbSet<ExperienceParallelRow> ExperienceParallelRoles => Set<ExperienceParallelRow>();

    public DbSet<ProjectRow> Projects => Set<ProjectRow>();

    public DbSet<ProjectTranslationRow> ProjectTranslations => Set<ProjectTranslationRow>();

    public DbSet<ProjectTechnologyRow> ProjectTechnologies => Set<ProjectTechnologyRow>();

    public DbSet<ProjectSourceRow> ProjectSources => Set<ProjectSourceRow>();

    public DbSet<SkillCategoryRow> SkillCategories => Set<SkillCategoryRow>();

    public DbSet<SkillCategoryTranslationRow> SkillCategoryTranslations => Set<SkillCategoryTranslationRow>();

    public DbSet<SkillItemRow> SkillItems => Set<SkillItemRow>();

    public DbSet<EducationRow> Education => Set<EducationRow>();

    public DbSet<EducationTranslationRow> EducationTranslations => Set<EducationTranslationRow>();

    public DbSet<SocialLinkRow> SocialLinks => Set<SocialLinkRow>();

    public DbSet<ContentSeedRow> ContentSeeds => Set<ContentSeedRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Identifier lengths. A language code is a two-letter tag; ids are slugs authored in
        // content/. Bounded columns document the intent and stop a typo becoming a 2 KB primary key.
        const int IdLength = 64;
        const int LanguageLength = 5;

        modelBuilder.Entity<ProfileRow>(b =>
        {
            b.ToTable("profiles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Email).HasMaxLength(320).IsRequired();
            b.Property(x => x.Availability).HasMaxLength(32).IsRequired();
            b.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.SpokenLanguages).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileTranslationRow>(b =>
        {
            b.ToTable("profile_translations");
            b.HasKey(x => new { x.ProfileId, x.LanguageCode });
            b.Property(x => x.ProfileId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Headline).HasMaxLength(300).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Location).HasMaxLength(200).IsRequired();
            b.Property(x => x.SummaryTemplate).IsRequired();
        });

        modelBuilder.Entity<SpokenLanguageRow>(b =>
        {
            b.ToTable("spoken_languages");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProfileId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Level).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.ProfileId, x.LanguageCode, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<ExperienceRow>(b =>
        {
            b.ToTable("experiences");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.Property(x => x.Company).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Ordinal);

            // A period must be a period. The database refuses an end before a start even if a bug
            // upstream tries, because a negative duration would render as nonsense on a public page.
            b.ToTable(t => t.HasCheckConstraint(
                "ck_experiences_period",
                "(end_year * 12 + end_month) >= (start_year * 12 + start_month)"));
            b.ToTable(t => t.HasCheckConstraint("ck_experiences_start_month", "start_month BETWEEN 1 AND 12"));
            b.ToTable(t => t.HasCheckConstraint("ck_experiences_end_month", "end_month BETWEEN 1 AND 12"));

            b.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Highlights).WithOne().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Technologies).WithOne().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Teams).WithOne().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ParallelRoles).WithOne().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExperienceTranslationRow>(b =>
        {
            b.ToTable("experience_translations");
            b.HasKey(x => new { x.ExperienceId, x.LanguageCode });
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Role).HasMaxLength(200).IsRequired();
            b.Property(x => x.EmploymentType).HasMaxLength(50);
        });

        modelBuilder.Entity<ExperienceHighlightRow>(b =>
        {
            b.ToTable("experience_highlights");
            b.HasKey(x => x.Id);
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Text).IsRequired();
            b.HasIndex(x => new { x.ExperienceId, x.LanguageCode, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<ExperienceTechnologyRow>(b =>
        {
            b.ToTable("experience_technologies");
            b.HasKey(x => x.Id);
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength);
            b.Property(x => x.Technology).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.ExperienceId, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<ExperienceTeamRow>(b =>
        {
            b.ToTable("experience_teams");
            b.HasKey(x => x.Id);
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Team).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.ExperienceId, x.LanguageCode, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<ExperienceParallelRow>(b =>
        {
            b.ToTable("experience_parallel_roles");
            b.HasKey(x => new { x.ExperienceId, x.ParallelExperienceId });
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength);
            b.Property(x => x.ParallelExperienceId).HasMaxLength(IdLength);
        });

        modelBuilder.Entity<ProjectRow>(b =>
        {
            b.ToTable("projects");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.Property(x => x.ExperienceId).HasMaxLength(IdLength).IsRequired();

            // Every project belongs to a role. Restrict rather than cascade: deleting a role that
            // still has projects is a mistake worth surfacing, not a silent loss of content.
            b.HasOne<ExperienceRow>()
                .WithMany()
                .HasForeignKey(x => x.ExperienceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.ExperienceId);
            b.HasIndex(x => x.Ordinal);
            b.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Technologies).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Sources).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectTranslationRow>(b =>
        {
            b.ToTable("project_translations");
            b.HasKey(x => new { x.ProjectId, x.LanguageCode });
            b.Property(x => x.ProjectId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Client).HasMaxLength(200);
            b.Property(x => x.Sector).HasMaxLength(100);
            b.Property(x => x.Summary).IsRequired();
        });

        modelBuilder.Entity<ProjectTechnologyRow>(b =>
        {
            b.ToTable("project_technologies");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProjectId).HasMaxLength(IdLength);
            b.Property(x => x.Technology).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.ProjectId, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<ProjectSourceRow>(b =>
        {
            b.ToTable("project_sources");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProjectId).HasMaxLength(IdLength);
            b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            b.HasIndex(x => new { x.ProjectId, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<SkillCategoryRow>(b =>
        {
            b.ToTable("skill_categories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.HasIndex(x => x.Ordinal);
            b.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SkillCategoryTranslationRow>(b =>
        {
            b.ToTable("skill_category_translations");
            b.HasKey(x => new { x.CategoryId, x.LanguageCode });
            b.Property(x => x.CategoryId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Label).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SkillItemRow>(b =>
        {
            b.ToTable("skill_items");
            b.HasKey(x => x.Id);
            b.Property(x => x.CategoryId).HasMaxLength(IdLength);
            b.Property(x => x.Item).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.CategoryId, x.Ordinal }).IsUnique();
        });

        modelBuilder.Entity<EducationRow>(b =>
        {
            b.ToTable("education");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.EducationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationTranslationRow>(b =>
        {
            b.ToTable("education_translations");
            b.HasKey(x => new { x.EducationId, x.LanguageCode });
            b.Property(x => x.EducationId).HasMaxLength(IdLength);
            b.Property(x => x.LanguageCode).HasMaxLength(LanguageLength);
            b.Property(x => x.Degree).HasMaxLength(200).IsRequired();
            b.Property(x => x.Institution).HasMaxLength(200);
            b.Property(x => x.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<SocialLinkRow>(b =>
        {
            b.ToTable("social_links");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(IdLength);
            b.Property(x => x.Label).HasMaxLength(100).IsRequired();
            b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            b.Property(x => x.Display).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Ordinal);
        });

        modelBuilder.Entity<ContentSeedRow>(b =>
        {
            b.ToTable("content_seeds");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        });
    }
}
