import { ServiceClientBase } from './base.js';
import type {
  Subject,
  TimeState,
  TimeLayoutItem,
  ClassPlan,
  Profile,
} from '../types/lesson.js';

export class LessonServiceClient extends ServiceClientBase {
  // #region Current

  async getCurrentSubject(): Promise<Subject | null> {
    return this._getOrNull<Subject>('/lesson/current/subject');
  }

  async getNextClassSubject(): Promise<Subject> {
    return this._get<Subject>('/lesson/current/next-class-subject');
  }

  async getCurrentState(): Promise<TimeState> {
    return this._get<TimeState>('/lesson/current/state');
  }

  async getCurrentTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/current/time-layout-item');
  }

  async getCurrentClassPlan(): Promise<ClassPlan | null> {
    return this._getOrNull<ClassPlan>('/lesson/current/class-plan');
  }

  async getCurrentSelectedIndex(): Promise<number> {
    return this._get<number>('/lesson/current/selected-index');
  }

  async getIsClassPlanEnabled(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-class-plan-enabled');
  }

  async getIsClassPlanLoaded(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-class-plan-loaded');
  }

  async getIsLessonConfirmed(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-lesson-confirmed');
  }

  // #endregion

  // #region Next

  async getNextClassTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/next/class-time-layout-item');
  }

  async getNextBreakingTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/next/breaking-time-layout-item');
  }

  // #endregion

  // #region Previous

  async getPreviousClassSubject(): Promise<Subject | null> {
    return this._getOrNull<Subject>('/lesson/previous/class-subject');
  }

  async getPreviousClassTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/previous/class-time-layout-item');
  }

  async getPreviousBreakingTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>(
      '/lesson/previous/breaking-time-layout-item',
    );
  }

  // #endregion

  // #region Timer

  async getOnClassLeftTime(): Promise<string> {
    return this._get<string>('/lesson/timer/on-class-left');
  }

  async getOnBreakingLeftTime(): Promise<string> {
    return this._get<string>('/lesson/timer/on-breaking-left');
  }

  async getElapsedSincePreviousClass(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-class');
  }

  async getElapsedSincePreviousBreaking(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-breaking');
  }

  async getElapsedSincePreviousAny(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-any');
  }

  // #endregion

  // #region Profile

  async getCurrentProfilePath(): Promise<string> {
    return this._get<string>('/lesson/profile/current-profile-path');
  }

  async getIsCurrentProfileTrusted(): Promise<boolean> {
    return this._get<boolean>('/lesson/profile/is-trusted');
  }

  async getProfile(): Promise<Profile> {
    return this._get<Profile>('/lesson/profile');
  }

  // #endregion
}
