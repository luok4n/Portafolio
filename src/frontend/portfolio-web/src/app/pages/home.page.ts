import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';

import { CachedNotice } from '../layout/cached-notice';
import { HeroSection } from '../sections/hero.section';
import { AboutSection } from '../sections/about.section';
import { ExperienceSection } from '../sections/experience.section';
import { ProjectsSection } from '../sections/projects.section';
import { SkillsSection } from '../sections/skills.section';
import { EngineeringSummarySection } from '../sections/engineering-summary.section';
import { ContactSection, EducationSection } from '../sections/education-contact.section';
import { ContentService } from '../core/content.service';
import { LocaleService } from '../core/locale.service';
import { SeoService } from '../core/seo.service';
import { pathFor } from '../core/locale';

/**
 * One page with anchored sections, which is the shape a recruiter expects and keeps the content
 * from being fragmented across routes for SEO. The detail pages are separate because they carry
 * enough of their own content to be worth indexing.
 */
@Component({
  selector: 'app-home-page',
  imports: [
    CachedNotice,
    HeroSection,
    AboutSection,
    ExperienceSection,
    ProjectsSection,
    SkillsSection,
    EngineeringSummarySection,
    EducationSection,
    ContactSection,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-cached-notice />
    <app-hero />
    <app-about />
    <app-experience />
    <app-projects />
    <app-skills />
    <app-engineering-summary />
    <app-education />
    <app-contact />
  `,
})
export class HomePage {
  private readonly content = inject(ContentService);
  private readonly localeService = inject(LocaleService);
  private readonly seo = inject(SeoService);

  constructor() {
    effect(() => {
      const locale = this.localeService.locale();
      const profile = this.content.profile();
      const path = pathFor(locale, 'home');

      this.seo.apply({
        locale,
        title: `${profile.name} — ${profile.title}`,
        // The first sentence of the summary, so the search snippet is the same claim the page makes.
        description: profile.summary.split('. ')[0] + '.',
        path,
        alternates: this.localeService.alternates(path),
        type: 'profile',
        jsonLd: SeoService.personJsonLd({
          name: profile.name,
          title: profile.title,
          location: profile.location,
          email: profile.email,
          sameAs: this.content.socialLinks().map((link) => link.url),
          skills: this.content.skills().flatMap((category) => category.items).slice(0, 25),
          alumniOf: this.content.education()[0]?.institution ?? '',
        }),
      });
    });
  }
}
