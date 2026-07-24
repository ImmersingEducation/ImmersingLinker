import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ClassServiceClient } from '../../src/clients/class.js';
import { NotFoundError, ConflictError, ImmersingLinkerError } from '../../src/clients/base.js';
import { Gender } from '../../src/types/common.js';

function createResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : status === 409 ? 'Conflict' : 'OK',
    text: async () => (body !== undefined ? JSON.stringify(body) : ''),
  } as Response;
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

const mockClass = {
  guid: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
  name: 'Class A',
  students: [],
  groupingRules: [],
  extraProperties: [],
};

const mockClassInfo = { guid: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee', name: 'Class A' };

const mockStudent = {
  guid: '11111111-2222-3333-4444-555555555555',
  name: 'Student 1',
  studentIdInClass: 1,
  gender: Gender.Male,
  extraProperties: [],
};

const mockExtraProp = {
  application: { uniqueId: 'myapp', name: 'My App' },
  name: 'key',
  value: 'val',
};

const mockGroupingRuleResponse = {
  guid: 'gggggggg-hhhh-iiii-jjjj-kkkkkkkkkkkk',
  name: 'Group 1',
  groups: [
    { guid: '11111111-1111-1111-1111-111111111111', name: 'Team A', contains: [] },
  ],
  unassignedStudentGuids: [],
};

describe('ClassServiceClient', () => {
  const client = new ClassServiceClient(5000);

  describe('GET endpoints', () => {
    it('getAllClasses returns Class[]', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockClass]));
      const result = await client.getAllClasses();
      expect(result).toEqual([mockClass]);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class');
    });

    it('getAllClassInfos returns ClassInfo[]', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockClassInfo]));
      const result = await client.getAllClassInfos();
      expect(result).toEqual([mockClassInfo]);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/infos');
    });

    it('getClassByGuid returns Class on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockClass));
      const result = await client.getClassByGuid('test-guid');
      expect(result).toEqual(mockClass);
    });

    it('getClassByGuid returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getClassByGuid('nonexistent');
      expect(result).toBeNull();
    });

    it('getStudentsByClassGuid returns Student[]', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockStudent]));
      const result = await client.getStudentsByClassGuid('test-guid');
      expect(result).toEqual([mockStudent]);
    });

    it('getStudentsByClassGuid returns [] on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getStudentsByClassGuid('nonexistent');
      expect(result).toEqual([]);
    });

    it('getStudentByStudentIdInClass returns Student on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockStudent));
      const result = await client.getStudentByStudentIdInClass('test-guid', 1);
      expect(result).toEqual(mockStudent);
    });

    it('getStudentByStudentIdInClass returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getStudentByStudentIdInClass('test-guid', 99);
      expect(result).toBeNull();
    });

    it('getExtraPropertiesByStudentIdInClass returns array', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockExtraProp]));
      const result = await client.getExtraPropertiesByStudentIdInClass('test-guid', 1);
      expect(result).toEqual([mockExtraProp]);
    });

    it('getExtraPropertiesByStudentIdInClass returns [] on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getExtraPropertiesByStudentIdInClass('test-guid', 1);
      expect(result).toEqual([]);
    });

    it('getExtraPropertiesByStudentIdAndAppIdInClass returns array', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockExtraProp]));
      const result = await client.getExtraPropertiesByStudentIdAndAppIdInClass('test-guid', 1, 'myapp');
      expect(result).toEqual([mockExtraProp]);
    });

    it('getExtraPropertyByNameAndStudentIdInClass returns prop on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const result = await client.getExtraPropertyByNameAndStudentIdInClass('test-guid', 1, 'myapp', 'key');
      expect(result).toEqual(mockExtraProp);
    });

    it('getExtraPropertyByNameAndStudentIdInClass returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getExtraPropertyByNameAndStudentIdInClass('test-guid', 1, 'myapp', 'missing');
      expect(result).toBeNull();
    });

    it('getExtraPropertiesByClassGuid returns array', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockExtraProp]));
      const result = await client.getExtraPropertiesByClassGuid('test-guid');
      expect(result).toEqual([mockExtraProp]);
    });

    it('getExtraPropertiesByClassGuid returns [] on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getExtraPropertiesByClassGuid('nonexistent');
      expect(result).toEqual([]);
    });

    it('getExtraPropertiesByAppIdInClass returns array', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockExtraProp]));
      const result = await client.getExtraPropertiesByAppIdInClass('test-guid', 'myapp');
      expect(result).toEqual([mockExtraProp]);
    });

    it('getExtraPropertyByAppIdAndNameInClass returns prop on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const result = await client.getExtraPropertyByAppIdAndNameInClass('test-guid', 'myapp', 'key');
      expect(result).toEqual(mockExtraProp);
    });

    it('getExtraPropertyByAppIdAndNameInClass returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getExtraPropertyByAppIdAndNameInClass('test-guid', 'myapp', 'missing');
      expect(result).toBeNull();
    });

    it('getGroupingRules returns GroupingRuleResponse[]', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockGroupingRuleResponse]));
      const result = await client.getGroupingRules('test-guid');
      expect(result).toEqual([mockGroupingRuleResponse]);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRule');
    });

    it('getGroupingRules returns [] on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getGroupingRules('nonexistent');
      expect(result).toEqual([]);
    });

    it('getGroupingRule returns GroupingRuleResponse on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockGroupingRuleResponse));
      const result = await client.getGroupingRule('test-guid', 'rule-guid');
      expect(result).toEqual(mockGroupingRuleResponse);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRule/rule-guid');
    });

    it('getGroupingRule returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getGroupingRule('test-guid', 'nonexistent');
      expect(result).toBeNull();
    });
  });

  describe('POST endpoints', () => {
    it('createClass sends request and returns Class', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockClass));
      const result = await client.createClass({ name: 'Class A' });
      expect(result).toEqual(mockClass);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'Class A' }),
      }));
    });

    it('addStudent sends request and returns Student', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockStudent));
      const request = { name: 'Student 1', studentIdInClass: 1, gender: Gender.Male };
      const result = await client.addStudent('test-guid', request);
      expect(result).toEqual(mockStudent);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/student', expect.objectContaining({
        method: 'POST',
      }));
    });

    it('addStudent throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.addStudent('bad-guid', { name: 'S', studentIdInClass: 1, gender: Gender.Male }))
        .rejects.toThrow(NotFoundError);
    });

    it('addStudent throws on 409', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(409));
      await expect(client.addStudent('test-guid', { name: 'S', studentIdInClass: 1, gender: Gender.Male }))
        .rejects.toThrow(ConflictError);
    });

    it('addClassExtraProperty sends request and returns prop', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const req = { appId: 'myapp', name: 'key', value: 'val' };
      const result = await client.addClassExtraProperty('test-guid', req);
      expect(result).toEqual(mockExtraProp);
    });

    it('addStudentExtraProperty sends request and returns prop', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const req = { appId: 'myapp', name: 'key', value: 'val' };
      const result = await client.addStudentExtraProperty('test-guid', 1, req);
      expect(result).toEqual(mockExtraProp);
    });

    it('addGroupingRule sends request and returns GroupingRuleResponse', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockGroupingRuleResponse));
      const result = await client.addGroupingRule('test-guid', { name: 'New Rule' });
      expect(result).toEqual(mockGroupingRuleResponse);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'New Rule' }),
      }));
    });

    it('addGroup sends request and returns GroupingRuleResponse', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockGroupingRuleResponse));
      const result = await client.addGroup('test-guid', 'rule-guid', { name: 'Team B' });
      expect(result).toEqual(mockGroupingRuleResponse);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules/rule-guid', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'Team B' }),
      }));
    });
  });

  describe('PUT endpoints', () => {
    it('updateClass sends request and returns Class', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockClass));
      const result = await client.updateClass('test-guid', { name: 'Updated' });
      expect(result).toEqual(mockClass);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid', expect.objectContaining({
        method: 'PUT',
      }));
    });

    it('updateClass throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updateClass('nonexistent', { name: 'N' })).rejects.toThrow(NotFoundError);
    });

    it('updateStudent sends request and returns Student', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockStudent));
      const result = await client.updateStudent('test-guid', 1, { name: 'Updated', gender: Gender.Female, groupInClass: '' });
      expect(result).toEqual(mockStudent);
    });

    it('updateStudent throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updateStudent('test-guid', 99, { name: 'N', gender: Gender.Male, groupInClass: '' }))
        .rejects.toThrow(NotFoundError);
    });

    it('updateClassExtraProperty sends request and returns prop', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const result = await client.updateClassExtraProperty('test-guid', 'myapp', 'key', { value: 'newval' });
      expect(result).toEqual(mockExtraProp);
    });

    it('updateClassExtraProperty throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updateClassExtraProperty('test-guid', 'myapp', 'missing', { value: 'v' }))
        .rejects.toThrow(NotFoundError);
    });

    it('updateStudentExtraProperty sends request and returns prop', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockExtraProp));
      const result = await client.updateStudentExtraProperty('test-guid', 1, 'myapp', 'key', { value: 'newval' });
      expect(result).toEqual(mockExtraProp);
    });

    it('updateGroupingRule sends request and returns GroupingRuleResponse', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockGroupingRuleResponse));
      const result = await client.updateGroupingRule('test-guid', 'rule-guid', { name: 'Renamed' });
      expect(result).toEqual(mockGroupingRuleResponse);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules/rule-guid', expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ name: 'Renamed' }),
      }));
    });

    it('updateGroupingRule throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updateGroupingRule('test-guid', 'nonexistent', { name: 'N' }))
        .rejects.toThrow(NotFoundError);
    });

    it('updateGroup sends request and returns GroupingRuleResponse', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockGroupingRuleResponse));
      const result = await client.updateGroup('test-guid', 'rule-guid', 'group-guid', { name: 'Renamed' });
      expect(result).toEqual(mockGroupingRuleResponse);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules/rule-guid/group-guid', expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ name: 'Renamed' }),
      }));
    });

    it('updateGroup throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updateGroup('test-guid', 'rule-guid', 'nonexistent', { name: 'N' }))
        .rejects.toThrow(NotFoundError);
    });
  });

  describe('DELETE endpoints', () => {
    it('deleteClass deletes and does not throw on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteClass('test-guid')).resolves.toBeUndefined();
    });

    it('deleteStudent deletes and does not throw on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteStudent('test-guid', 1)).resolves.toBeUndefined();
    });

    it('deleteStudent throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteStudent('test-guid', 99)).rejects.toThrow(NotFoundError);
    });

    it('deleteClassExtraProperty deletes on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteClassExtraProperty('test-guid', 'myapp', 'key')).resolves.toBeUndefined();
    });

    it('deleteClassExtraProperty throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteClassExtraProperty('test-guid', 'myapp', 'missing')).rejects.toThrow(NotFoundError);
    });

    it('deleteStudentExtraProperty deletes on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteStudentExtraProperty('test-guid', 1, 'myapp', 'key')).resolves.toBeUndefined();
    });

    it('deleteStudentExtraProperty throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteStudentExtraProperty('test-guid', 1, 'myapp', 'missing')).rejects.toThrow(NotFoundError);
    });

    it('deleteGroupingRule deletes on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteGroupingRule('test-guid', 'rule-guid')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules/rule-guid', expect.objectContaining({
        method: 'DELETE',
      }));
    });

    it('deleteGroupingRule throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteGroupingRule('test-guid', 'nonexistent')).rejects.toThrow(NotFoundError);
    });

    it('deleteGroup deletes on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deleteGroup('test-guid', 'rule-guid', 'group-guid')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/class/test-guid/groupingRules/rule-guid/group-guid', expect.objectContaining({
        method: 'DELETE',
      }));
    });

    it('deleteGroup throws on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteGroup('test-guid', 'rule-guid', 'nonexistent')).rejects.toThrow(NotFoundError);
    });
  });

  describe('Offline factory methods', () => {
    it('createClassOffline creates Class with GUID', () => {
      const cls = ClassServiceClient.createClassOffline('Test Class');
      expect(cls.guid).toBeDefined();
      expect(cls.guid.length).toBeGreaterThan(0);
      expect(cls.name).toBe('Test Class');
      expect(cls.students).toEqual([]);
      expect(cls.groupingRules).toEqual([]);
      expect(cls.extraProperties).toEqual([]);
    });

    it('createStudentOffline creates Student with GUID', () => {
      const student = ClassServiceClient.createStudentOffline('Test Student', 1, Gender.Male);
      expect(student.guid).toBeDefined();
      expect(student.guid.length).toBeGreaterThan(0);
      expect(student.name).toBe('Test Student');
      expect(student.studentIdInClass).toBe(1);
      expect(student.gender).toBe(Gender.Male);
      expect(student.extraProperties).toEqual([]);
    });

    it('createClassExtraPropertyOffline creates prop with correct fields', () => {
      const prop = ClassServiceClient.createClassExtraPropertyOffline('myapp', 'key', 'val');
      expect(prop.application.uniqueId).toBe('myapp');
      expect(prop.name).toBe('key');
      expect(prop.value).toBe('val');
    });

    it('createStudentExtraPropertyOffline creates prop with correct fields', () => {
      const prop = ClassServiceClient.createStudentExtraPropertyOffline('myapp', 'score', 95);
      expect(prop.application.uniqueId).toBe('myapp');
      expect(prop.name).toBe('score');
      expect(prop.value).toBe(95);
    });
  });

  describe('error handling on GET with unexpected error', () => {
    it('should throw on 500 for getAllClasses', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getAllClasses()).rejects.toThrow(ImmersingLinkerError);
    });
  });
});
