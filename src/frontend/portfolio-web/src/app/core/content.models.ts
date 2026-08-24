/**
 * Mirrors of the API's response shapes.
 *
 * Written by hand rather than generated from the OpenAPI document: the contract is small and
 * stable, and a generator would be a build dependency and a code-generation step to maintain for
 * eight endpoints. If the contract grows, that trade flips.
 */

export interface LanguageInfo {
  requested: string;
  resolved: string;
  resolvedFrom: 'explicit' | 'accept-header' | 'fallback';
}

export interface SpokenLanguage {
  language: string;
  level: string;
}

export interface Profile {
  name: string;
  headline: string;
  title: string;
  location: string;
  email: string;
  openToWork: boolean;
  summary: string;
  yearsOfExperience: number;
  monthsOfExperience: number;
  languages: SpokenLanguage[];
}

export interface Experience {
  id: string;
  company: string;
  role: string;
  employmentType: string;
  /** Inclusive `YYYY-MM`. */
  start: string;
  end: string;
  durationMonths: number;
  /** True when this role was held alongside another; shown explicitly so it does not read as a data error. */
  concurrent: boolean;
  parallelWith: string[];
  teams: string[];
  technologies: string[];
  highlights: string[];
  projectIds: string[];
}

export interface ProjectSource {
  url: string;
  checked: string;
}

export interface Project {
  id: string;
  name: string;
  client: string;
  experienceId: string;
  company: string;
  sector: string;
  featured: boolean;
  /** False when no public source backs the description. The page shows no sources block rather than a badge. */
  verified: boolean;
  summary: string;
  contribution: string | null;
  technologies: string[];
  sources: ProjectSource[];
}

export interface SkillCategory {
  id: string;
  label: string;
  items: string[];
}

export interface Education {
  id: string;
  degree: string;
  institution: string;
  location: string;
  year: number;
}

export interface SocialLink {
  id: string;
  label: string;
  url: string;
  display: string;
}

export interface PortfolioContent {
  language: LanguageInfo;
  profile: Profile;
  experience: Experience[];
  projects: Project[];
  skills: SkillCategory[];
  education: Education[];
  socialLinks: SocialLink[];
}

/** Where the content on screen came from. Drives the discreet "cached" notice; never an error screen. */
export type ContentOrigin = 'api' | 'snapshot';

export interface ContentState {
  content: PortfolioContent;
  origin: ContentOrigin;
}
