/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DifficultyRank } from './DifficultyRank';
import type { TaskStatus } from './TaskStatus';
import type { TaskType } from './TaskType';
import type { User } from './User';
export type TodoTask = {
    id: number;
    userId: number;
    user: User;
    title: string;
    description?: string | null;
    type: TaskType;
    difficultyRank: DifficultyRank;
    status: TaskStatus;
    createdAt: string;
    isRepeatable: boolean;
    completionCount: number;
    lastCompletedAt?: string | null;
};

