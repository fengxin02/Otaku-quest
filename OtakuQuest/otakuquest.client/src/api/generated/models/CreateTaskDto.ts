/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DifficultyRank } from './DifficultyRank';
import type { TaskType } from './TaskType';
export type CreateTaskDto = {
    title: string;
    description?: string | null;
    type: TaskType;
    difficultyRank: DifficultyRank;
    isRepeatable: boolean;
};

